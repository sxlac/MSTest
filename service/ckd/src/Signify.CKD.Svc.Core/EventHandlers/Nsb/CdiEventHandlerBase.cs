using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.CKD.Svc.Core.BusinessRules;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.Constants;
using Signify.CKD.Svc.Core.Data.Entities;
using Signify.CKD.Svc.Core.Events;
using Signify.CKD.Svc.Core.Exceptions;
using Signify.CKD.Svc.Core.Models;
using Signify.CKD.Svc.Core.Queries;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Signify.CKD.Svc.Core.Infrastructure.Observability;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public abstract class CdiEventHandlerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly IPublishObservability _publishObservability;
    private readonly IPayableRules _payableRules;

    protected ILogger Logger { get; }

    protected CdiEventHandlerBase(ILogger logger,
        IMediator mediator,
        IMapper mapper,
        IPublishObservability publishObservability,
        IPayableRules payableRules)
    {
        Logger = logger;

        _mediator = mediator;
        _mapper = mapper;
        _publishObservability = publishObservability;
        _payableRules = payableRules;
    }

    /// <summary>
    /// Handles the given event from CDI, paying the provider if it meets certain qualifications
    /// </summary>
    /// <param name="message"></param>
    /// <param name="exam"></param>
    /// <param name="context"></param>
    protected async Task Handle(CdiEventBase message, CKD exam, IMessageHandlerContext context)
    {
        PublishObservabilityEvent(Observability.ProviderPay.ExpiredKits, message, exam);
        
        // this is the null check pattern. It checks if the IsMet property of IsProviderPayable(exam)'s output is false and then assigns the output to rulesCheckResult
        if (IsProviderPayable(exam) is {IsMet: false} rulesCheckResult)
        {
            await PublishStatus(context, message, CKDStatusCode.ProviderNonPayableEventReceived, rulesCheckResult.Reason);

            Logger.LogInformation("Exam with EvaluationId={EvaluationId}, EventId={EventId} does not satisfy business rules to pay provider, with Reason={Reason}",
                message.EvaluationId, message.RequestId, rulesCheckResult.Reason);

            _publishObservability.Commit();
            return;
        }

        await PublishStatus(context, message, CKDStatusCode.ProviderPayableEventReceived);
        await SendProviderPayRequest(message, exam, context);

        _publishObservability.Commit();
    }

    /// <summary>
    /// Gets the exam including its statuses
    /// </summary>
    /// <exception cref="ExamNotFoundException" />
    protected async Task<CKD> GetExam(CdiEventBase message)
    {
        var exam = await _mediator.Send(new GetCKD
        {
            EvaluationId = message.EvaluationId
        });

        return exam ?? throw new ExamNotFoundException(message.EvaluationId, message.RequestId);
    }

    protected async Task<bool> IsPerformed(CKD ckd)
    {
        var statuses = await _mediator.Send(new GetCKDStatuses
        {
            CKDId = ckd.CKDId
        });

        var status = statuses.First(each =>
            each.CKDStatusCodeId == CKDStatusCode.CKDPerformed.CKDStatusCodeId ||
            each.CKDStatusCodeId == CKDStatusCode.CKDNotPerformed.CKDStatusCodeId);

        return status.CKDStatusCodeId == CKDStatusCode.CKDPerformed.CKDStatusCodeId;
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
    protected Task PublishStatus(IPipelineContext context, CdiEventBase message, CKDStatusCode statusCode, string reason = null)
#pragma warning restore CA1822
    {
        var statusEvent = new ProviderPayStatusEvent
        {
            EvaluationId = message.EvaluationId,
            EventId = message.RequestId,
            StatusCode = statusCode,
            StatusDateTime = message.DateTime,
            ParentCdiEvent = message.GetType().Name,
            Reason = reason
        };

        return context.SendLocal(statusEvent);
    }

    /// <summary>
    /// Check the business rules. to be completed as part of ANC-2994
    /// </summary>
    /// <param name="exam"></param>
    /// <returns></returns>
    private BusinessRuleStatus IsProviderPayable(CKD exam)
    {
        var answers = new PayableRuleAnswers
        {
            ExpirationDate = exam.ExpirationDate,
            DateOfService = exam.DateOfService,
            CkdAnswer = exam.CKDAnswer,
            IsPerformed = true
        };
        return _payableRules.IsPayable(answers);
    }

    /// <summary>
    /// Send NSB event to handle database write and kafka event
    /// </summary>
    /// <param name="message"></param>
    /// <param name="ckd"></param>
    /// <param name="context"></param>
    [Trace]
    private Task SendProviderPayRequest(CdiEventBase message, CKD ckd, IPipelineContext context)
    {
        var providerPayEventRequest = _mapper.Map<ProviderPayRequest>(ckd);
        providerPayEventRequest.AdditionalDetails = new Dictionary<string, string>
        {
            { "EvaluationId", message.EvaluationId.ToString() },
            { "AppointmentId", ckd.AppointmentId.ToString() },
            { "ExamId", ckd.CKDId.ToString() }
        };
        return context.SendLocal(providerPayEventRequest);
    }

    /// <summary>
    /// Method to emit New Relic Custom Attributes.
    /// Calling the method at the end of the Handler so that multiple events are not emitted in case of errors and NSB retries.
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="message"></param>
    /// <param name="exam"></param>
    /// <param name="sendImmediate"></param>

    [Trace]
    private void PublishObservabilityEvent(string eventType, CdiEventBase message, CKD exam, bool sendImmediate = false)
    {
        var observabilityEvent = new ObservabilityEvent
        {
            EvaluationId = message.EvaluationId,
            EventId = message.RequestId,
            EventType = eventType,
            EventValue = new Dictionary<string, object>
            {
                { Observability.EventParams.EvaluationId, exam.EvaluationId },
                { Observability.EventParams.ExpirationDateNullAttribute, !exam.ExpirationDate.HasValue },
                { Observability.EventParams.DateOfServiceNullAttribute, !exam.DateOfService.HasValue },
                { Observability.EventParams.ExpirationAfterDateOfServiceAttribute, _payableRules.IsAnswerExpirationDateAfterDateOfService(new PayableRuleAnswers
                {
                    ExpirationDate = exam.ExpirationDate,
                    DateOfService = exam.DateOfService
                }).IsMet}
            }
        };

        _publishObservability.RegisterEvent(observabilityEvent, sendImmediate);
    }
}
