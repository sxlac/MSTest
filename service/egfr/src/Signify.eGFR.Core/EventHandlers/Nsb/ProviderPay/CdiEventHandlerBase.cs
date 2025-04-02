using AutoMapper;
using EgfrNsbEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Refit;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.eGFR.Core.ApiClients.EvaluationApi.Responses;
using Signify.eGFR.Core.ApiClients.EvaluationApi;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Constants;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Events.Akka;
using Signify.eGFR.Core.Events.Status;
using Signify.eGFR.Core.Exceptions;
using Signify.eGFR.Core.Infrastructure;
using Signify.eGFR.Core.Queries;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public abstract class CdiEventHandlerBase(
    ILogger logger,
    IMediator mediator,
    ITransactionSupplier transactionSupplier,
    IPublishObservability publishObservability,
    IApplicationTime applicationTime,
    IMapper mapper,
    IEvaluationApi evaluationApi) : BaseNsbHandler(logger, mediator, transactionSupplier, publishObservability, applicationTime)
{
    /// <summary>
    /// Handles the given event from CDI, paying the provider if it meets certain qualifications
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cdiEventType"></param>
    /// <param name="context"></param>
    protected async Task Handle(CdiEventBase message, ExamStatusCode cdiEventType, IMessageHandlerContext context)
    {
        using var transaction = TransactionSupplier.BeginTransaction();
        var exam = await GetExam(message, context.CancellationToken);
        if (exam is null)
        {
            await HandleNullExam(message.EvaluationId, message.RequestId);
            return;
        }

        if (!IsPerformed(exam))
        {
            Logger.LogInformation("Nothing to do, exam was not performed, for EvaluationId={EvaluationId}, RequestId={RequestId}",
                message.EvaluationId, message.RequestId);
            return;
        }

        await PublishStatusEventAsync(message, cdiEventType, exam, context.CancellationToken);
        
        if (cdiEventType == ExamStatusCode.CdiFailedWithoutPayReceived)
        {
            await PublishStatusEventAsync(message, ExamStatusCode.ProviderNonPayableEventReceived, exam, context.CancellationToken, "PayProvider is false for the CDIFailedEvent");
            await CommitTransactions(transaction, context.CancellationToken);
            return;
        }

        if (!await DidLabResultsArrive(exam.EvaluationId,  context.CancellationToken))
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
    /// <exception cref="ExamNotFoundByEvaluationException" />
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
    /// Decide what to do if exam is null based on the Canceled Evaluation workflow. ANC-4194.
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

        throw new ExamNotFoundByEvaluationException(evaluationId, eventId);
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
    
    /// <summary>
    /// Checks if exam contains Performed status
    /// </summary>
    /// <param name="exam"></param>
    /// <returns></returns>
    protected static bool IsPerformed(Exam exam)
    {
        var status = exam.ExamStatuses.First(each =>
            each.ExamStatusCodeId == ExamStatusCode.ExamPerformed.StatusCodeId || each.ExamStatusCodeId == ExamStatusCode.ExamNotPerformed.StatusCodeId);
        return status.ExamStatusCodeId == ExamStatusCode.ExamPerformed.StatusCodeId;
    }

    /// <summary>
    /// Check if LabResults were received by looking at the database for ResultDataDownloaded status
    /// </summary>
    /// <param name="evaluationId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<bool> DidLabResultsArrive(long evaluationId, CancellationToken cancellationToken)
    {
        var kedLabResult =  await Mediator.Send(new QueryLabResultByEvaluationId(evaluationId), cancellationToken);
        if (kedLabResult != null)
            return true;
        
        return await Mediator.Send(new QueryQuestLabResultByEvaluationId(evaluationId), cancellationToken) != null;
    }

    /// <summary>
    /// Invoke <see cref="ExamStatusEvent"/> as a Nsb event to write to database and optionally raise kafka event
    /// </summary>
    /// <param name="message"></param>
    /// <param name="statusCode"></param>
    /// <param name="exam"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="reason"></param>
    private Task PublishStatusEventAsync(CdiEventBase message, ExamStatusCode statusCode, Exam exam, CancellationToken cancellationToken, string reason = null)
    {
        var status = mapper.Map<ProviderPayStatusEvent>(message);
        mapper.Map(exam, status);
        status.StatusCode = statusCode;
        status.Reason = reason;
        var updateStatus = new UpdateExamStatus
        {
            ExamStatus = status
        };

        return Mediator.Send(updateStatus, cancellationToken);
    }
    
    /// <summary>
    /// Send NSB event to handle database write and kafka event
    /// </summary>
    /// <param name="message"></param>
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
}