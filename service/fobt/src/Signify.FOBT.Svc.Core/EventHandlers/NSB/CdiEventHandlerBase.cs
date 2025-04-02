using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Refit;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.FOBT.Svc.Core.ApiClient;
using Signify.FOBT.Svc.Core.ApiClient.Response;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Constants;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Exceptions;
using Signify.FOBT.Svc.Core.Infrastructure;
using Signify.FOBT.Svc.Core.Infrastructure.Observability;
using Signify.FOBT.Svc.Core.Models;
using Signify.FOBT.Svc.Core.Queries;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public abstract class CdiEventHandlerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    protected ILogger Logger { get; }
    private readonly ITransactionSupplier _transactionSupplier;
    private readonly IPublishObservability _publishObservability;
    private readonly IEvaluationApi _evaluationApi;
    private readonly IApplicationTime _applicationTime;

    protected CdiEventHandlerBase(ILogger logger,
        IMediator mediator,
        IMapper mapper,
        ITransactionSupplier transactionSupplier,
        IPublishObservability publishObservability,
        IEvaluationApi evaluationApi,
        IApplicationTime applicationTime)
    {
        Logger = logger;
        _mediator = mediator;
        _mapper = mapper;
        _transactionSupplier = transactionSupplier;
        _publishObservability = publishObservability;
        _evaluationApi = evaluationApi;
        _applicationTime = applicationTime;
    }

    /// <summary>
    /// Handles the given event from CDI, paying the provider if it meets certain qualifications
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cdiEventType"></param>
    /// <param name="context"></param>
    [Transaction]
    protected async Task Handle(CdiEventBase message, FOBTStatusCode cdiEventType, IMessageHandlerContext context)
    {
        using var transaction = _transactionSupplier.BeginTransaction();
        var exam = await GetExam(message);
        if (exam is null)
        {
            await HandleNullExam(message.EvaluationId, message.RequestId);
            await CommitTransactions(transaction);
            return;
        }

        var statuses = await GetExamStatuses(exam.FOBTId);
        if (!IsPerformed(statuses))
        {
            Logger.LogInformation("Nothing to do, exam was not performed, for EvaluationId={EvaluationId}, RequestId={RequestId}",
                message.EvaluationId, message.RequestId);
            return;
        }

        await PublishStatus(message, cdiEventType, exam);
        if (Equals(cdiEventType, FOBTStatusCode.CdiFailedWithoutPayReceived))
        {
            await PublishStatus(message, FOBTStatusCode.ProviderNonPayableEventReceived, exam, "PayProvider is false for the CDIFailedEvent");
            await CommitTransactions(transaction);
            return;
        }

        if (!IsValidOrInvalidLabResultsReceived(statuses))
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
        var evalStatusHistory = await _evaluationApi.GetEvaluationStatusHistory(evaluationId).ConfigureAwait(false);

        ValidateStatusHistory(evaluationId, evalStatusHistory);

        var finalizedEvent = evalStatusHistory.Content?.FirstOrDefault(s => s.EvaluationStatusCodeId == EvaluationStatus.EvaluationFinalized);
        var canceledEvent = evalStatusHistory.Content?.FirstOrDefault(s => s.EvaluationStatusCodeId == EvaluationStatus.EvaluationCanceled);

        if (canceledEvent is not null)
        {
            Logger.LogInformation(
                "Exam with EvaluationId={EvaluationId}, EventId={EventId} was Canceled at-least once", evaluationId, eventId);
            PublishObservabilityEvent(evaluationId, canceledEvent.CreatedDateTime.ToUniversalTime(), Observability.Evaluation.EvaluationCanceledEvent,
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
            PublishObservabilityEvent(evaluationId, finalizedEvent.CreatedDateTime.ToUniversalTime(), Observability.Evaluation.MissingEvaluationEvent,
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
            failReason = $"Api request to {nameof(_evaluationApi.GetEvaluationStatusHistory)} failed with HttpStatusCode: {evalStatusHistory.StatusCode}";
            additionalDetails = new Dictionary<string, object>
            {
                { Observability.EventParams.Message, failReason }
            };
        }
        else if (evalStatusHistory.Content is null)
        {
            failReason = $"Empty response from {nameof(_evaluationApi.GetEvaluationStatusHistory)}";
            additionalDetails = new Dictionary<string, object>
            {
                { Observability.EventParams.Message, failReason }
            };
        }

        PublishObservabilityEvent(evaluationId, _applicationTime.UtcNow(), Observability.ApiIssues.ExternalApiFailureEvent, additionalDetails, true);
        throw new EvaluationApiRequestException(evaluationId, evalStatusHistory.StatusCode, failReason, evalStatusHistory.Error);
    }

    /// <summary>
    /// Check if ValidLabResultsReceived Or InvalidLabResultsReceived FOBTStatus is present
    /// </summary>
    /// <param name="statuses"></param>
    /// <returns></returns>
    [Trace]
    private static bool IsValidOrInvalidLabResultsReceived(IEnumerable<FOBTStatus> statuses)
    {
        var statusFound = statuses.Any(each =>
            each.FOBTStatusCodeId == FOBTStatusCode.ValidLabResultsReceived.FOBTStatusCodeId ||
            each.FOBTStatusCodeId == FOBTStatusCode.InvalidLabResultsReceived.FOBTStatusCodeId);

        return statusFound;
    }

    /// <summary>
    /// Gets the exam based off the evaluation id
    /// </summary>
    [Trace]
    protected async Task<FOBT> GetExam(CdiEventBase message)
    {
        var exam = await _mediator.Send(new GetFOBT { EvaluationId = message.EvaluationId });

        return exam;
    }

    /// <summary>
    /// Gets the exam statuses
    /// </summary>
    [Trace]
    private async Task<List<FOBTStatus>> GetExamStatuses(int examId)
    {
        var examStatuses = await _mediator.Send(new QueryExamStatuses { ExamId = examId });

        return examStatuses;
    }

    /// <summary>
    /// Checks if exam contains Performed status
    /// </summary>
    /// <param name="statuses"></param>
    /// <returns></returns>
    [Trace]
    protected static bool IsPerformed(IEnumerable<FOBTStatus> statuses)
    {
        var status = statuses.First(each =>
            each.FOBTStatusCodeId == FOBTStatusCode.FOBTPerformed.FOBTStatusCodeId ||
            each.FOBTStatusCodeId == FOBTStatusCode.FOBTNotPerformed.FOBTStatusCodeId);

        return status.FOBTStatusCodeId == FOBTStatusCode.FOBTPerformed.FOBTStatusCodeId;
    }

    /// <summary>
    /// Invoke <see cref="ExamStatusEvent"/> as a Nsb event to write to database and optionally raise kafka event
    /// </summary>
    /// <param name="message"></param>
    /// <param name="statusCode"></param>
    /// <param name="exam"></param>
    /// <param name="reason"></param>
    [Trace]
    private Task PublishStatus(CdiEventBase message, FOBTStatusCode statusCode, FOBT exam, string reason = null)
    {
        var statusEvent = new UpdateExamStatus
        {
            ExamStatus = new ProviderPayStatusEvent
            {
                Exam = exam,
                EvaluationId = message.EvaluationId,
                EventId = message.RequestId,
                StatusCode = statusCode,
                StatusDateTime = message.DateTime,
                ParentCdiEvent = message.GetType().Name,
                Reason = reason,
                ParentEventReceivedDateTime = message.ReceivedByFobtDateTime
            }
        };

        return _mediator.Send(statusEvent);
    }

    /// <summary>
    /// Send NSB event to handle database write and kafka event
    /// </summary>
    /// <param name="message"></param>
    /// <param name="exam"></param>
    /// <param name="context"></param>
    [Trace]
    private async Task SendProviderPayRequest(CdiEventBase message, FOBT exam, IPipelineContext context)
    {
        var providerPayEventRequest = _mapper.Map<ProviderPayRequest>(exam);
        providerPayEventRequest.EventId = message.RequestId;
        providerPayEventRequest.ParentEventDateTime = message.DateTime;
        providerPayEventRequest.ParentEventReceivedDateTime = message.ReceivedByFobtDateTime;
        providerPayEventRequest.ParentEvent = message.GetType().Name;
        providerPayEventRequest.AdditionalDetails = new Dictionary<string, string>
        {
            { "EvaluationId", message.EvaluationId.ToString() },
            { "AppointmentId", exam.AppointmentId.ToString() },
            { "ExamId", exam.FOBTId.ToString() }
        };
        await context.SendLocal(providerPayEventRequest);
    }

    /// <summary>
    /// Commit both TransactionSupplier and Observability transactions
    /// </summary>
    /// <param name="transaction">The transaction generated as part of TransactionSupplier.BeginTransaction</param>
    [Transaction]
    private async Task CommitTransactions(IBufferedTransaction transaction)
    {
        await transaction.CommitAsync();
        _publishObservability.Commit();
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
    private void PublishObservabilityEvent(long evaluationId, DateTime eventCreatedDateTime, string eventType,
        Dictionary<string, object> additionalDetails = null,
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

        _publishObservability.RegisterEvent(observabilityEvent, sendImmediate);
    }
}