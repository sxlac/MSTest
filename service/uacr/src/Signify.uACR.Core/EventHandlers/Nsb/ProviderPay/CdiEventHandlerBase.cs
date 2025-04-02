using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Refit;
using Signify.uACR.Core.ApiClients.EvaluationApi.Responses;
using Signify.uACR.Core.ApiClients.EvaluationApi;
using Signify.uACR.Core.Commands;
using Signify.uACR.Core.Constants;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Data;
using Signify.uACR.Core.Events.Akka;
using Signify.uACR.Core.Events.Status;
using Signify.uACR.Core.Exceptions;
using Signify.uACR.Core.Infrastructure;
using Signify.uACR.Core.Queries;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System;
using NsbEventHandlers;
using Signify.Dps.Observability.Library.Services;
using UacrNsbEvents;

namespace Signify.uACR.Core.EventHandlers.Nsb;

public abstract class CdiEventHandlerBase(
    ILogger logger,
    IMediator mediator,
    IMapper mapper,
    ITransactionSupplier transactionSupplier,
    IEvaluationApi evaluationApi,
    IPublishObservability publishObservability,
    IApplicationTime applicationTime) : BaseNsbHandler(logger, mediator, transactionSupplier, publishObservability, applicationTime)
{

    /// <summary>
    /// Handles the given event from CDI, paying the provider if it meets certain qualifications
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cdiEventType"></param>
    /// <param name="context"></param>
    [Transaction]
    protected async Task Handle(CdiEventBase message, ExamStatusCode cdiEventType, IMessageHandlerContext context)
    {
        using var transaction = TransactionSupplier.BeginTransaction();
        var exam = await GetExam(message, context.CancellationToken);
        if (exam is null)
        {
            await HandleNullExam(message.EvaluationId, message.RequestId);
            await CommitTransactions(transaction, context.CancellationToken);
            return;
        }

        if (!IsPerformed(exam))
        {
            Logger.LogInformation("Nothing to do, exam was not performed, for EvaluationId={EvaluationId}, RequestId={RequestId}",
                message.EvaluationId, message.RequestId);
            await CommitTransactions(transaction, context.CancellationToken);
            return;
        }

        await PublishStatusEventAsync(message, cdiEventType, exam);

        if (cdiEventType == ExamStatusCode.CdiFailedWithoutPayReceived)
        {
            await PublishStatusEventAsync(message, ExamStatusCode.ProviderNonPayableEventReceived, exam, "PayProvider is false for the CDIFailedEvent");
            await CommitTransactions(transaction, context.CancellationToken);
            return;
        }

        if (!await LabResultsArrived(exam.EvaluationId, context.CancellationToken))
        {
            Logger.LogInformation("Awaiting LabResult for Exam with EvaluationId={EvaluationId}, EventId={EventId}", message.EvaluationId, message.RequestId);
            await CommitTransactions(transaction, context.CancellationToken);
            return;
        }

        await SendProviderPayRequest(message, exam, context);

        await CommitTransactions(transaction, context.CancellationToken);
    }

    /// <summary>
    /// Gets the exam 
    /// </summary>
    [Trace]
    protected async Task<Exam> GetExam(CdiEventBase message, CancellationToken token)
    {
        var exam = await Mediator.Send(new QueryExamByEvaluation
        {
            EvaluationId = message.EvaluationId,
            IncludeStatuses = true
        }, token);
        return exam;
    }

    /// <summary>
    /// Checks if exam contains Performed status
    /// </summary>
    /// <param name="exam"></param>
    /// <returns></returns>
    [Trace]
    protected static bool IsPerformed(Exam exam)
    {
        var status = exam.ExamStatuses.First(each =>
            each.ExamStatusCodeId == ExamStatusCode.ExamPerformed.ExamStatusCodeId ||
            each.ExamStatusCodeId == ExamStatusCode.ExamNotPerformed.ExamStatusCodeId);
        return status.ExamStatusCodeId == ExamStatusCode.ExamPerformed.ExamStatusCodeId;
    }

    /// <summary>
    /// Check if LabResults were received by looking at the database for ResultDataDownloaded status
    /// </summary>
    /// <param name="evaluationId"></param>
    /// <returns></returns>
    [Trace]
    private async Task<bool> LabResultsArrived(long evaluationId, CancellationToken token)
    {
        var results = await Mediator.Send(new QueryLabResultByEvaluationId(evaluationId), token);
        return results != null;
    }

    /// <summary>
    /// Invoke <see cref="ExamStatusEvent"/> as a Nsb event to write to database and optionally raise kafka event
    /// </summary>
    /// <param name="message"></param>
    /// <param name="statusCode"></param>
    /// <param name="exam"></param>
    /// <param name="reason"></param>
    [Trace]
    private Task PublishStatusEventAsync(CdiEventBase message, ExamStatusCode statusCode, Exam exam, string reason = null)
    {
        var status = mapper.Map<ProviderPayStatusEvent>(message);
        mapper.Map(exam, status);
        status.StatusCode = statusCode;
        status.Reason = reason;
        var updateStatus = new UpdateExamStatus
        {
            ExamStatus = status
        };

        return Mediator.Send(updateStatus);
    }

    /// <summary>
    /// Send NSB event to handle database write and kafka event
    /// </summary>
    /// <param name="message">CDI event</param>
    /// <param name="exam"></param>
    /// <param name="context"></param>
    [Trace]
    private async Task SendProviderPayRequest(CdiEventBase message, Exam exam, IPipelineContext context)
    {
        var providerPayEventRequest = mapper.Map<ProviderPayRequest>(exam);
        mapper.Map(message, providerPayEventRequest);
        providerPayEventRequest.ParentEventDateTime = message.DateTime;
        providerPayEventRequest.AdditionalDetails = new Dictionary<string, string>
        {
            { "EvaluationId", exam.EvaluationId.ToString() },
            { "AppointmentId", exam.AppointmentId.ToString() },
            { "ExamId", exam.ExamId.ToString() }
        };
        await context.SendLocal(providerPayEventRequest);
    }

    /// <summary>
    /// Decide what to do if exam is null based on the Canceled Evaluation workflow.
    /// </summary>
    /// <param name="evaluationId"></param>
    /// <param name="eventId"></param>
    /// <exception cref="ExamNotFoundException"></exception>
    [Trace]
    private async Task HandleNullExam(long evaluationId, Guid eventId)
    {
        var evalStatusHistory = await evaluationApi.GetEvaluationStatusHistory(evaluationId).ConfigureAwait(false);

        ValidateStatusHistory(evaluationId, evalStatusHistory);

        var finalizedEvent = evalStatusHistory.Content?.FirstOrDefault(s => s.EvaluationStatusCodeId == EvaluationStatus.EvaluationFinalized);
        var canceledEvent = evalStatusHistory.Content?.FirstOrDefault(s => s.EvaluationStatusCodeId == EvaluationStatus.EvaluationCanceled);

        if (canceledEvent is not null)
        {
            Logger.LogInformation(
                "Exam with EvaluationId={EvaluationId}, EventId={EventId} was Canceled at-least once", evaluationId, eventId);
            PublishObservabilityEvents(evaluationId, canceledEvent.CreatedDateTime.ToUniversalTime(), Observability.Evaluation.EvaluationCanceledEvent,
                sendImmediate: true);

            // if event was canceled but not finalized
            if (finalizedEvent is null)
            {
                return;
            }
        }

        if (finalizedEvent is not null)
        {
            Logger.LogInformation(
                "Exam with EvaluationId={EvaluationId}, EventId={EventId} was Finalized but still missing from database", evaluationId, eventId);
            PublishObservabilityEvents(evaluationId, finalizedEvent.CreatedDateTime.ToUniversalTime(), Observability.Evaluation.MissingEvaluationEvent,
                sendImmediate: true);
        }

        throw new ExamNotFoundException(evaluationId, eventId);
    }

    /// <summary>
    /// Validate the response from EvaluationApi Request
    /// </summary>
    /// <param name="evaluationId"></param>
    /// <param name="evalStatusHistory"></param>
    /// <exception cref="EvaluationApiRequestException"></exception>
    [Trace]
    private void ValidateStatusHistory(long evaluationId, IApiResponse<IList<EvaluationStatusHistory>> evalStatusHistory)
    {
        if (evalStatusHistory.StatusCode == HttpStatusCode.OK && evalStatusHistory.Content is not null)
            return;

        var failReason = string.Empty;
        var additionalDetails = new Dictionary<string, object>();

        if (evalStatusHistory.StatusCode != HttpStatusCode.OK)
        {
            failReason = $"Api request to {nameof(evaluationApi.GetEvaluationStatusHistory)} failed with HttpStatusCode: {evalStatusHistory.StatusCode}";
            additionalDetails = new Dictionary<string, object>
            {
                { Observability.EventParams.Message, failReason }
            };
        }
        else if (evalStatusHistory.Content is null)
        {
            failReason = $"Empty response from {nameof(evaluationApi.GetEvaluationStatusHistory)}";
            additionalDetails = new Dictionary<string, object>
            {
                { Observability.EventParams.Message, failReason }
            };
        }

        PublishObservabilityEvents(evaluationId, ApplicationTime.UtcNow(), Observability.ApiIssues.ExternalApiFailureEvent, additionalDetails, true);
        throw new EvaluationApiRequestException(evaluationId, evalStatusHistory.StatusCode, failReason, evalStatusHistory.Error);
    }
}