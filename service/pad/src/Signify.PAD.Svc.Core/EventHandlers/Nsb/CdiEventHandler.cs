using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Refit;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Svc.Core.ApiClient;
using Signify.PAD.Svc.Core.ApiClient.Response;
using Signify.PAD.Svc.Core.BusinessRules;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Constants;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.Core.Exceptions;
using Signify.PAD.Svc.Core.Infrastructure;
using Signify.PAD.Svc.Core.Models;
using Signify.PAD.Svc.Core.Queries;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public class CdiEventHandler : IHandleMessages<CDIPassedEvent>, IHandleMessages<CDIFailedEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<CdiEventHandler> _logger;
    private readonly IMapper _mapper;
    private readonly IPayableRules _payableRules;
    private readonly IEvaluationApi _evaluationApi;
    private readonly IPublishObservability _publishObservability;
    private readonly IApplicationTime _applicationTime;

    public CdiEventHandler(ILogger<CdiEventHandler> logger, IMediator mediator, IMapper mapper, IPayableRules payableRules, IEvaluationApi evaluationApi,
        IPublishObservability publishObservability, IApplicationTime applicationTime)
    {
        _logger = logger;
        _mediator = mediator;
        _mapper = mapper;
        _payableRules = payableRules;
        _evaluationApi = evaluationApi;
        _publishObservability = publishObservability;
        _applicationTime = applicationTime;
    }

    [Transaction]
    public async Task Handle(CDIPassedEvent message, IMessageHandlerContext context)
    {
        _logger.LogDebug("Start {Handler} for {Event} with EvaluationId={EvaluationId}, EventId={EventId}",
            nameof(CdiEventHandler), nameof(CDIPassedEvent), message.EvaluationId, message.RequestId);

        var exam = await GetExam(message, context.CancellationToken);
        if (exam is null)
        {
            await HandleNullExam(message.EvaluationId, message.RequestId);
            return;
        }

        if (!await IsPerformed(exam, context.CancellationToken))
        {
            _logger.LogInformation("Nothing to do, exam was not performed, for EvaluationId={EvaluationId}, RequestId={RequestId}",
                message.EvaluationId, message.RequestId);
            return;
        }

        await PublishStatusEventAsync(context, message, exam.PADId, PADStatusCode.CdiPassedReceived);

        // this is the null check pattern. It checks if the result of method's IsMet is false and then assigns the method's result to rulesStatus
        if (IsProviderPayable(exam) is { IsMet: false } rulesCheckResult)
        {
            await PublishStatusEventAsync(context, message, exam.PADId, PADStatusCode.ProviderNonPayableEventReceived, rulesCheckResult.Reason);
            _logger.LogInformation(
                "Exam with EvaluationId={EvaluationId}, EventId={EventId} does not satisfy the business rules: {Reason}, hence abandoning ProviderPay",
                message.EvaluationId, message.RequestId, rulesCheckResult.Reason);
            return;
        }

        await PublishStatusEventAsync(context, message, exam.PADId, PADStatusCode.ProviderPayableEventReceived);
        await SendProviderPayRequestAsync(message, exam, context);

        _logger.LogDebug("End {Handler} for {Event} with EvaluationId={EvaluationId}, EventId={EventId}",
            nameof(CdiEventHandler), nameof(CDIPassedEvent), message.EvaluationId, message.RequestId);
    }

    [Transaction]
    public async Task Handle(CDIFailedEvent message, IMessageHandlerContext context)
    {
        _logger.LogDebug("Start {Handler} for {Event} with EvaluationId={EvaluationId}, EventId={EventId}",
            nameof(CdiEventHandler), nameof(CDIPassedEvent), message.EvaluationId, message.RequestId);

        var exam = await GetExam(message, context.CancellationToken);
        if (exam is null)
        {
            await HandleNullExam(message.EvaluationId, message.RequestId);
            return;
        }

        if (!await IsPerformed(exam, context.CancellationToken))
        {
            _logger.LogInformation("Nothing to do, exam was not performed, for EvaluationId={EvaluationId}, RequestId={RequestId}",
                message.EvaluationId, message.RequestId);
            return;
        }

        var cdiStatus = message.PayProvider ? PADStatusCode.CdiFailedWithPayReceived : PADStatusCode.CdiFailedWithoutPayReceived;
        await PublishStatusEventAsync(context, message, exam.PADId, cdiStatus);
        if (!message.PayProvider)
        {
            await PublishStatusEventAsync(context, message, exam.PADId, PADStatusCode.ProviderNonPayableEventReceived,
                "PayProvider is false for the CDIFailedEvent");
            _logger.LogInformation(
                "Exam with EvaluationId={EvaluationId}, EventId={EventId} for Event={Event} with PayProvider set to {PayProvider}, hence abandoning ProviderPay",
                message.EvaluationId, message.RequestId, nameof(CDIFailedEvent), message.PayProvider);
            return;
        }

        // this is the null check pattern. It checks if the result of method's IsMet is false and then assigns the method's result to rulesStatus
        if (IsProviderPayable(exam) is { IsMet: false } rulesCheckResult)
        {
            await PublishStatusEventAsync(context, message, exam.PADId, PADStatusCode.ProviderNonPayableEventReceived, rulesCheckResult.Reason);
            _logger.LogInformation(
                "Exam with EvaluationId={EvaluationId}, EventId={EventId} does not satisfy the business rules: {Reason}, hence abandoning ProviderPay",
                message.EvaluationId, message.RequestId, rulesCheckResult.Reason);
            return;
        }

        await PublishStatusEventAsync(context, message, exam.PADId, PADStatusCode.ProviderPayableEventReceived);
        await SendProviderPayRequestAsync(message, exam, context);

        _logger.LogDebug("End {Handler} for {Event} with EvaluationId={EvaluationId}, EventId={EventId}",
            nameof(CdiEventHandler), nameof(CDIPassedEvent), message.EvaluationId, message.RequestId);
    }

    /// <summary>
    /// Gets the exam based off the evaluation id 
    /// </summary>
    [Trace]
    private async Task<PAD> GetExam(CdiEventBase message, CancellationToken cancellationToken)
    {
        var exam = await _mediator.Send(new GetPAD
        {
            EvaluationId = message.EvaluationId
        }, cancellationToken);

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
        var evalStatusHistory = await _evaluationApi.GetEvaluationStatusHistory(evaluationId).ConfigureAwait(false);

        ValidateStatusHistory(evaluationId, evalStatusHistory);

        var finalizedEvent = evalStatusHistory.Content?.FirstOrDefault(s => s.EvaluationStatusCodeId == EvaluationStatus.EvaluationFinalized);
        var canceledEvent = evalStatusHistory.Content?.FirstOrDefault(s => s.EvaluationStatusCodeId == EvaluationStatus.EvaluationCanceled);

        if (canceledEvent is not null)
        {
            _logger.LogInformation(
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
            _logger.LogInformation(
                "Exam with EvaluationId={EvaluationId}, EventId={EventId} was Finalized but still missing from database", evaluationId, eventId);
            PublishObservability(evaluationId, finalizedEvent.CreatedDateTime.ToUniversalTime(), Observability.Evaluation.MissingEvaluationEvent,
                sendImmediate: true);
        }

        throw new ExamNotFoundException(evaluationId, eventId);
    }

    [Trace]
    private async Task<bool> IsPerformed(PAD entity, CancellationToken cancellationToken)
    {
        var status = await _mediator.Send(new QueryPadPerformedStatus(entity.PADId), cancellationToken);
        if (!status.IsPerformed.HasValue)
            throw new InvalidOperationException($"Unable to determine whether PAD was Performed or Not, for EvaluationId={entity.EvaluationId}");

        return status.IsPerformed.Value;
    }

    /// <summary>
    /// Invoke <see cref="ExamStatusEvent"/> as a Nsb event to write to database and optionally raise kafka event
    /// </summary>
    /// <param name="context">IMessageHandlerContext</param>
    /// <param name="message"></param>
    /// <param name="examId"></param>
    /// <param name="statusCode"></param>
    /// <param name="reason"></param>
    [Trace]
    private static async Task PublishStatusEventAsync(IMessageHandlerContext context, CdiEventBase message, int examId, PADStatusCode statusCode,
        string reason = null)
    {
        var statusEvent = new ProviderPayStatusEvent
        {
            EvaluationId = message.EvaluationId,
            EventId = message.RequestId,
            ExamId = examId,
            StatusCode = statusCode,
            StatusDateTime = message.DateTime,
            ParentCdiEvent = message.GetType().Name,
            Reason = reason
        };

        await context.SendLocal(statusEvent);
    }

    /// <summary>
    /// Check the business rules.
    /// </summary>
    /// <param name="exam"></param>
    /// <returns></returns>
    [Trace]
    private BusinessRuleStatus IsProviderPayable(PAD exam)
    {
        var answers = new PayableRuleAnswers { LeftNormalityIndicator = exam.LeftNormalityIndicator, RightNormalityIndicator = exam.RightNormalityIndicator };
        return _payableRules.IsPayable(answers);
    }

    /// <summary>
    /// Send NSB event to handle database write and kafka event
    /// </summary>
    /// <param name="message"></param>
    /// <param name="pad"></param>
    /// <param name="context"></param>
    [Trace]
    private async Task SendProviderPayRequestAsync(CdiEventBase message, PAD pad, IMessageHandlerContext context)
    {
        var providerPayEventRequest = _mapper.Map<ProviderPayRequest>(pad);
        providerPayEventRequest.AdditionalDetails = new Dictionary<string, string>
        {
            { "EvaluationId", message.EvaluationId.ToString() },
            { "AppointmentId", pad.AppointmentId.ToString() },
            { "ExamId", pad.PADId.ToString() }
        };
        await context.SendLocal(providerPayEventRequest);
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

        PublishObservability(evaluationId, _applicationTime.UtcNow(), Observability.ApiIssues.ExternalApiFailureEvent, additionalDetails, true);
        throw new EvaluationApiRequestException(evaluationId, evalStatusHistory.StatusCode, failReason, evalStatusHistory.Error);
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

        _publishObservability.RegisterEvent(observabilityEvent, sendImmediate);
    }
}