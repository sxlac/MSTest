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
using Signify.HBA1CPOC.Messages.Events;
using Signify.HBA1CPOC.Messages.Events.Status;
using Signify.HBA1CPOC.Svc.Core.ApiClient;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Response;
using Signify.HBA1CPOC.Svc.Core.BusinessRules;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Constants;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.Svc.Core.Exceptions;
using Signify.HBA1CPOC.Svc.Core.Infrastructure;
using Signify.HBA1CPOC.Svc.Core.Infrastructure.Observability;
using Signify.HBA1CPOC.Svc.Core.Models;
using Signify.HBA1CPOC.Svc.Core.Queries;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public abstract class CdiEventHandlerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly IPayableRules _payableRules;
    private readonly IPublishObservability _publishObservability;
    private readonly IEvaluationApi _evaluationApi;
    private readonly IApplicationTime _applicationTime;

    protected ILogger Logger { get; }

    protected CdiEventHandlerBase(ILogger logger,
        IMediator mediator,
        IMapper mapper,
        IPayableRules payableRules,
        IPublishObservability publishObservability,
        IEvaluationApi evaluationApi, 
        IApplicationTime applicationTime)
    {
        Logger = logger;

        _mediator = mediator;
        _mapper = mapper;
        _payableRules = payableRules;
        _publishObservability = publishObservability;
        _evaluationApi = evaluationApi;
        _applicationTime = applicationTime;
    }

    protected async Task Handle(BaseCdiEvent message, HBA1CPOC exam, IMessageHandlerContext context)
    {
        var additionalDetails = new Dictionary<string, object>
        {
            { Observability.EventParams.ExpirationDateNullAttribute, !exam.ExpirationDate.HasValue },
            { Observability.EventParams.DateOfServiceNullAttribute, !exam.DateOfService.HasValue },
            { Observability.EventParams.ExpirationAfterDateOfServiceAttribute, _payableRules.IsAnswerExpirationDateAfterDateOfService(new PayableRuleAnswers
            {
                ExpirationDate = exam.ExpirationDate,
                DateOfService = exam.DateOfService
            }).IsMet}
        
        };
        PublishObservability(message.EvaluationId,_applicationTime.UtcNow(),Observability.ProviderPay.InvalidKits, additionalDetails);

        
        if (IsProviderPayable(exam) is {IsMet: false} rulesCheckResult)
        {
            await PublishStatus(context, message, HBA1CPOCStatusCode.ProviderNonPayableEventReceived, rulesCheckResult.Reason);

            Logger.LogInformation("Exam with EvaluationId={EvaluationId}, EventId={EventId} does not satisfy business rules to pay provider, with Reason={Reason}",
                message.EvaluationId, message.RequestId, rulesCheckResult.Reason);

            _publishObservability.Commit();
            return;
        }

        await PublishStatus(context, message, HBA1CPOCStatusCode.ProviderPayableEventReceived);
        await SendProviderPayRequestAsync(message, exam, context);

        _publishObservability.Commit();
    }

    /// <summary>
    /// Gets the exam including its statuses
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    protected async Task<HBA1CPOC> GetExam(BaseCdiEvent message)
    {
        var exam = await _mediator.Send(new GetHBA1CPOC
        {
            EvaluationId = message.EvaluationId
        });

        return exam;
    }

    protected async Task<bool> IsPerformed(HBA1CPOC entity)
    {
        var statuses = await _mediator.Send(new QueryExamStatuses
        {
            ExamId = entity.HBA1CPOCId
        });

        var status = statuses.First(each =>
            each.HBA1CPOCStatusCodeId == HBA1CPOCStatusCode.HBA1CPOCPerformed.HBA1CPOCStatusCodeId ||
            each.HBA1CPOCStatusCodeId == HBA1CPOCStatusCode.HBA1CPOCNotPerformed.HBA1CPOCStatusCodeId);

        return status.HBA1CPOCStatusCodeId == HBA1CPOCStatusCode.HBA1CPOCPerformed.HBA1CPOCStatusCodeId;
    }

    /// <summary>
    /// Invoke <see cref="ExamStatusEvent"/> as a Nsb event to write to database and optionally raise kafka event
    /// </summary>
    /// <param name="context"></param>
    /// <param name="message"></param>
    /// <param name="statusCode"></param>
    /// <param name="reason"></param>
#pragma warning disable CA1822 // Method does not access instance data, can be marked as static - This will change later to send the status event to MediatR instead of NSB, so it will need to be instanced
    // ReSharper disable once MemberCanBeMadeStatic.Global
    protected Task PublishStatus(IMessageHandlerContext context, BaseCdiEvent message, HBA1CPOCStatusCode statusCode, string reason = null)
#pragma warning restore CA1822
    {
        var statusEvent = new ProviderPayStatusEvent
        {
            EvaluationId = message.EvaluationId,
            EventId = message.RequestId,
            StatusCode = statusCode.HBA1CPOCStatusCodeId,
            StatusDateTime = message.DateTime,
            ParentCdiEvent = message.GetType().Name,
            Reason = reason
        };

        return context.SendLocal(statusEvent);
    }

    /// <summary>
    /// Check the business rules
    /// </summary>
    /// <param name="exam"></param>
    /// <returns></returns>
    [Trace]
    private BusinessRuleStatus IsProviderPayable(HBA1CPOC exam)
    {
        var answers = new PayableRuleAnswers
        {
            ExpirationDate = exam.ExpirationDate,
            DateOfService = exam.DateOfService,
            NormalityIndicator = _mapper.Map<Normality>(exam.NormalityIndicator) 
        };
        return _payableRules.IsPayable(answers);
    }

    /// <summary>
    /// Send NSB event to handle database write and kafka event
    /// </summary>
    /// <param name="message"></param>
    /// <param name="exam"></param>
    /// <param name="context"></param>
    [Trace]
    private async Task SendProviderPayRequestAsync(BaseCdiEvent message, HBA1CPOC exam, IPipelineContext context)
    {
        var providerPayEventRequest = _mapper.Map<ProviderPayRequest>(exam);
        providerPayEventRequest.AdditionalDetails = new Dictionary<string, string>
        {
            { "EvaluationId", message.EvaluationId.ToString() },
            { "AppointmentId", exam.AppointmentId.ToString() },
            { "ExamId", exam.HBA1CPOCId.ToString() }
        };
        await context.SendLocal(providerPayEventRequest);
    }
    
        /// <summary>
    /// Decide what to do if exam is null based on the Canceled Evaluation workflow. ANC-4194.
    /// </summary>
    /// <param name="evaluationId"></param>
    /// <param name="eventId"></param>
    /// <exception cref="ExamNotFoundException"></exception>
    [Trace]
        protected async Task HandleNullExam(long evaluationId, Guid eventId)
    {
        var evalStatusHistory = await _evaluationApi.GetEvaluationStatusHistory(evaluationId).ConfigureAwait(false);

        ValidateStatusHistory(evaluationId, evalStatusHistory);

        var finalizedEvent =
            evalStatusHistory.Content?.FirstOrDefault(s =>
                s.EvaluationStatusCodeId == EvaluationStatus.EvaluationFinalized);
        var canceledEvent =
            evalStatusHistory.Content?.FirstOrDefault(s =>
                s.EvaluationStatusCodeId == EvaluationStatus.EvaluationCanceled);

        if (canceledEvent is not null)
        {
            Logger.LogInformation(
                "Exam with EvaluationId={EvaluationId}, EventId={EventId} was Canceled at-least once", evaluationId,
                eventId);
            PublishObservability(evaluationId, canceledEvent.CreatedDateTime.ToUniversalTime(),
                Observability.Evaluation.EvaluationCanceledEvent,
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
                "Exam with EvaluationId={EvaluationId}, EventId={EventId} was Finalized but still missing from database",
                evaluationId, eventId);
            PublishObservability(evaluationId, finalizedEvent.CreatedDateTime.ToUniversalTime(),
                Observability.Evaluation.MissingEvaluationEvent,
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
    private void ValidateStatusHistory(long evaluationId,
        IApiResponse<IList<EvaluationStatusHistory>> evalStatusHistory)
    {
        if (evalStatusHistory.StatusCode == HttpStatusCode.OK && evalStatusHistory.Content is not null)
            return;

        var failReason = string.Empty;
        var additionalDetails = new Dictionary<string, object>();

        if (evalStatusHistory.StatusCode != HttpStatusCode.OK)
        {
            failReason =
                $"Api request to {nameof(_evaluationApi.GetEvaluationStatusHistory)} failed with HttpStatusCode: {evalStatusHistory.StatusCode}";
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

        PublishObservability(evaluationId, _applicationTime.UtcNow(), Observability.ApiIssues.ExternalApiFailureEvent,
            additionalDetails, true);
        throw new EvaluationApiRequestException(evaluationId, evalStatusHistory.StatusCode, failReason,
            evalStatusHistory.Error);
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
    private void PublishObservability(long evaluationId, DateTime eventCreatedDateTime, string eventType,
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
                {
                    Observability.EventParams.CreatedDateTime,
                    new DateTimeOffset(eventCreatedDateTime, TimeSpan.Zero).ToUnixTimeSeconds()
                },
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
