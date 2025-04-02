using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Refit;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.DEE.Svc.Core.ApiClient.Responses;
using Signify.DEE.Svc.Core.ApiClient;
using Signify.DEE.Svc.Core.Commands;
using Signify.DEE.Svc.Core.Constants;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Events.Status;
using Signify.DEE.Svc.Core.Events;
using Signify.DEE.Svc.Core.Exceptions;
using Signify.DEE.Svc.Core.Infrastructure;
using Signify.DEE.Svc.Core.Messages.Queries;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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
    IMapper mapper,
    ITransactionSupplier transactionSupplier,
    IPublishObservability publishObservability,
    IEvaluationApi evaluationApi,
    IApplicationTime applicationTime)
{
    protected ILogger Logger { get; } = logger;

    /// <summary>
    /// Handles the given event from CDI, paying the provider if it meets certain qualifications
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cdiEventType"></param>
    /// <param name="context"></param>
    [Transaction]
    protected async Task Handle(CdiEventBase message, ExamStatusCode cdiEventType, IMessageHandlerContext context)
    {
        using var transaction = transactionSupplier.BeginTransaction();
        var exam = await GetExam(message);
        if (exam is null)
        {
            await HandleNullExam(message.EvaluationId, message.RequestId);
            await CommitTransactions(transaction);
            return;
        }

        if (!IsPerformed(exam))
        {
            Logger.LogInformation("Nothing to do, exam was not performed, for EvaluationId={EvaluationId}, RequestId={RequestId}",
                message.EvaluationId, message.RequestId);
            return;
        }

        await PublishStatus(message, cdiEventType, exam);
        if (cdiEventType == ExamStatusCode.CdiFailedWithoutPayReceived)
        {
            await PublishStatus(message, ExamStatusCode.ProviderNonPayableEventReceived, exam, "PayProvider is false for the CDIFailedEvent");
            await CommitTransactions(transaction);
            return;
        }

        if (!await DidOrderResultsArrive(exam.ExamId))
        {
            Logger.LogInformation("Awaiting OrderResult for Exam with EvaluationId={EvaluationId}, EventId={EventId}", message.EvaluationId, message.RequestId);
            await CommitTransactions(transaction);
            return;
        }

        await SendProviderPayRequest(message, exam, context);
        await CommitTransactions(transaction);
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
            PublishObservability(evaluationId, canceledEvent.CreatedDateTime.ToUniversalTime(), Observability.Evaluation.EvaluationCanceledEvent,
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
            PublishObservability(evaluationId, finalizedEvent.CreatedDateTime.ToUniversalTime(), Observability.Evaluation.MissingEvaluationEvent,
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

        PublishObservability(evaluationId, applicationTime.UtcNow(), Observability.ApiIssues.ExternalApiFailureEvent, additionalDetails, true);
        throw new EvaluationApiRequestException(evaluationId, evalStatusHistory.StatusCode, failReason, evalStatusHistory.Error);
    }

    /// <summary>
    /// Check if OrderResults were received by looking at the database for ResultDataDownloaded status
    /// </summary>
    /// <param name="examId"></param>
    /// <returns></returns>
    private async Task<bool> DidOrderResultsArrive(int examId)
    {
        var examStatus = await mediator.Send(new GetExamStatusModel(examId, ExamStatusCode.ResultDataDownloaded.ExamStatusCodeId));
        return examStatus != null;
    }

    /// <summary>
    /// Gets the exam including its statuses
    /// </summary>
    protected async Task<Exam> GetExam(CdiEventBase message)
    {
        var exam = await mediator.Send(new GetExamByEvaluation
        {
            EvaluationId = message.EvaluationId,
            IncludeStatuses = true
        });

        return exam;
    }

    /// <summary>
    /// Checks if exam contains Performed status
    /// </summary>
    /// <param name="exam"></param>
    /// <returns></returns>
    protected static bool IsPerformed(Exam exam)
    {
        var status = exam.ExamStatuses.First(each =>
            each.ExamStatusCodeId == ExamStatusCode.Performed.ExamStatusCodeId || each.ExamStatusCodeId == ExamStatusCode.NotPerformed.ExamStatusCodeId);
        return status.ExamStatusCodeId == ExamStatusCode.Performed.ExamStatusCodeId;
    }

    /// <summary>
    /// Invoke <see cref="Signify.DEE.Svc.Core.Commands.UpdateExamStatus"/> to write to database and optionally raise kafka event
    /// </summary>
    /// <param name="message"></param>
    /// <param name="statusCode"></param>
    /// <param name="exam"></param>
    /// <param name="reason"></param>
    private Task PublishStatus(CdiEventBase message, ExamStatusCode statusCode, Exam exam, string reason = null)
    {
        var status = mapper.Map<ProviderPayStatusEvent>(message);
        mapper.Map(exam, status);
        status.StatusCode = statusCode;
        status.Reason = reason;
        var updateStatus = new UpdateExamStatus
        {
            ExamStatus = status
        };

        return mediator.Send(updateStatus);
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
        providerPayEventRequest.AdditionalDetails = new Dictionary<string, string>
        {
            { "EvaluationId", exam.EvaluationId.ToString() },
            { "ExamId", exam.ExamId.ToString() }
        };
        await context.SendLocal(providerPayEventRequest);
    }

    /// <summary>
    /// Publish to Observability platform
    /// </summary>
    /// <param name="evaluationId">evaluationId of the exam</param>
    /// <param name="eventCreatedDateTime">datetime the event was created in UTC</param>
    /// <param name="eventType">type of observability event</param>
    /// <param name="additionalDetails"></param>
    /// <param name="sendImmediate">whether to publish event immediately or wait for a commit command</param>
    [Trace]
    private void PublishObservability(long evaluationId, DateTime eventCreatedDateTime, string eventType, Dictionary<string, object> additionalDetails = null,
        bool sendImmediate = false)
    {
        var observabilityEvent = new ObservabilityEvent
        {
            EvaluationId = evaluationId,
            EventType = eventType,
            EventValue = new Dictionary<string, object>
            {
                { Observability.EventParams.EvaluationId, evaluationId },
                { Observability.EventParams.CreatedDateTime, new DateTimeOffset(eventCreatedDateTime, TimeSpan.Zero).ToUnixTimeSeconds() },
            }
        };
        if (additionalDetails?.Count > 0)
        {
            foreach (var (key, value) in additionalDetails)
            {
                observabilityEvent.EventValue.Add(key, value);
            }
        }

        publishObservability.RegisterEvent(observabilityEvent, sendImmediate);
    }

    /// <summary>
    /// Commit both TransactionSupplier and Observability transactions
    /// </summary>
    /// <param name="transaction">The transaction generated as part of TransactionSupplier.BeginTransaction</param>
    [Transaction]
    private async Task CommitTransactions(IBufferedTransaction transaction)
    {
        await transaction.CommitAsync();
        publishObservability.Commit();
    }
}