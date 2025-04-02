using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.HBA1CPOC.Messages.Events.Akka;
using Signify.HBA1CPOC.Messages.Events.Status;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Constants;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.Svc.Core.Data;
using Signify.HBA1CPOC.Svc.Core.Infrastructure.Observability;
using Signify.HBA1CPOC.Svc.Core.Queries;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace Signify.HBA1CPOC.Svc.Core.EventHandlers;

public class ExamStatusHandler : IHandleMessages<ExamStatusEvent>
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly ITransactionSupplier _transactionSupplier;
    private readonly IPublishObservability _publishObservability;

    public ExamStatusHandler(ILogger<ExamStatusHandler> logger,
        IMapper mapper,
        IMediator mediator,
        ITransactionSupplier transactionSupplier,
        IPublishObservability publishObservability)
    {
        _mapper = mapper;
        _mediator = mediator;
        _transactionSupplier = transactionSupplier;
        _logger = logger;
        _publishObservability = publishObservability;
    }

    [Transaction]
    public async Task Handle(ExamStatusEvent message, IMessageHandlerContext context)
    {
        _logger.LogDebug("Start {Handle} for EvaluationId={EvaluationId}, EventId={EventId}",
            nameof(ExamStatusHandler), message.EvaluationId, message.EventId);

        using var transaction = _transactionSupplier.BeginTransaction();
        var exam = await GetExam(message.EvaluationId);
        await SaveStatusToDb(message, exam);
        var eventToPublish = CreateKafkaStatusEvent(message.StatusCode, message, exam);
        if (eventToPublish is not null)
        {
            await PublishStatusToKafka(message.EventId, eventToPublish);
        }

        await transaction.CommitAsync(context.CancellationToken);
        PublishObservabilityEvents(message);
        _logger.LogDebug("End {Handle} for EvaluationId={EvaluationId}", nameof(ExamStatusHandler), message.EvaluationId);
    }

    /// <summary>
    /// Get the HBA1CPOC exam details from database based on <see cref="evaluationId"/>
    /// </summary>
    /// <param name="evaluationId">EvaluationId of the corresponding PAD exam</param>
    /// <returns></returns>
    private async Task<Data.Entities.HBA1CPOC> GetExam(long evaluationId)
    {
        return await _mediator.Send(new GetHBA1CPOC { EvaluationId = evaluationId }, CancellationToken.None);
    }

    private Task<HBA1CPOCStatus> SaveStatusToDb(ExamStatusEvent message, Data.Entities.HBA1CPOC exam)
    {
        return _mediator.Send(new CreateHBA1CPOCStatus { HBA1CPOCId = exam.HBA1CPOCId, StatusCodeId = message.StatusCode },
            CancellationToken.None);
    }

    /// <summary>
    /// Create the event type based on the status code.
    /// Returns null if no kafka event is to be published.
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="message"></param>
    /// <param name="exam"></param>
    /// <returns></returns>
    private BaseStatusMessage CreateKafkaStatusEvent(int statusCode, ExamStatusEvent message, Data.Entities.HBA1CPOC exam)
    {
        switch (statusCode)
        {
            case var cdiPassedReceived when cdiPassedReceived == HBA1CPOCStatusCode.CdiPassedReceived.HBA1CPOCStatusCodeId:
            case var cdiFailedWithPayReceived when cdiFailedWithPayReceived == HBA1CPOCStatusCode.CdiFailedWithPayReceived.HBA1CPOCStatusCodeId:
            case var cdiFailedWithoutPayReceived when cdiFailedWithoutPayReceived == HBA1CPOCStatusCode.CdiFailedWithoutPayReceived.HBA1CPOCStatusCodeId:
                return null;
            case var providerPayRequestSent when providerPayRequestSent == HBA1CPOCStatusCode.ProviderPayRequestSent.HBA1CPOCStatusCodeId:
                var publishProviderPayRequestSent = _mapper.Map<ProviderPayRequestSent>(exam);
                publishProviderPayRequestSent.PaymentId = ((ProviderPayStatusEvent)message).PaymentId;
                publishProviderPayRequestSent.PdfDeliveryDate =
                    ((ProviderPayStatusEvent)message).StatusDateTime.UtcDateTime;
                return publishProviderPayRequestSent;
            case var payableEventReceived when payableEventReceived == HBA1CPOCStatusCode.ProviderPayableEventReceived.HBA1CPOCStatusCodeId:
                var payableEvent = _mapper.Map<ProviderPayableEventReceived>(exam);
                payableEvent.ParentCdiEvent = ((ProviderPayStatusEvent)message).ParentCdiEvent;
                return payableEvent;
            case var nonPayableEventReceived when nonPayableEventReceived == HBA1CPOCStatusCode.ProviderNonPayableEventReceived.HBA1CPOCStatusCodeId:
                var nonPayableEven = _mapper.Map<ProviderNonPayableEventReceived>(exam);
                nonPayableEven.ParentCdiEvent = ((ProviderPayStatusEvent)message).ParentCdiEvent;
                nonPayableEven.Reason = ((ProviderPayStatusEvent)message).Reason;
                return nonPayableEven;
            default:
                return null;
        }
    }

    private async Task PublishStatusToKafka(Guid eventId, BaseStatusMessage eventToPublish)
    {
        await _mediator.Send(new PublishStatusUpdate(eventId, eventToPublish));
    }

    /// <summary>
    /// Method to emit events to surface in observability dashboard.
    /// Calling the method at the end of the Handler so that multiple events are not emitted in case of errors and NSB retries.
    /// </summary>
    /// <param name="statusMessage"></param>
    [Trace]
    private void PublishObservabilityEvents(ExamStatusEvent statusMessage)
    {
        try
        {
            switch (statusMessage.StatusCode)
            {
                case var cdiPassedReceived when cdiPassedReceived == HBA1CPOCStatusCode.CdiPassedReceived.HBA1CPOCStatusCodeId:
                case var cdiFailedWithPayReceived when cdiFailedWithPayReceived == HBA1CPOCStatusCode.CdiFailedWithPayReceived.HBA1CPOCStatusCodeId:
                case var cdiFailedWithoutPayReceived when cdiFailedWithoutPayReceived == HBA1CPOCStatusCode.CdiFailedWithoutPayReceived.HBA1CPOCStatusCodeId:
                    Publish(Observability.ProviderPay.CdiEvents, Observability.EventParams.CdiEvent);
                    break;
                case var providerPayableEventReceived when providerPayableEventReceived == HBA1CPOCStatusCode.ProviderPayableEventReceived.HBA1CPOCStatusCodeId:
                    Publish(Observability.ProviderPay.PayableCdiEvents, Observability.EventParams.PayableCdiEvents);
                    break;
                case var nonPayableEventReceived when nonPayableEventReceived == HBA1CPOCStatusCode.ProviderNonPayableEventReceived.HBA1CPOCStatusCodeId:
                    Publish(Observability.ProviderPay.NonPayableCdiEvents, Observability.EventParams.NonPayableCdiEvents, new Dictionary<string, object>
                    {
                        { Observability.EventParams.NonPayableReason, ((ProviderPayStatusEvent)statusMessage).Reason }
                    });
                    break;
                case var providerPayRequestSent when providerPayRequestSent == HBA1CPOCStatusCode.ProviderPayRequestSent.HBA1CPOCStatusCodeId:
                    Publish(Observability.ProviderPay.PayableCdiEvents, Observability.EventParams.PayableCdiEvents, new Dictionary<string, object>
                    {
                        { Observability.EventParams.PaymentId, ((ProviderPayStatusEvent)statusMessage).PaymentId }
                    });
                    break;
            }

            void Publish(string eventType, string eventStatusParam, Dictionary<string, object> additionalEventDetails = null)
            {
                var observabilityEvent = new ObservabilityEvent
                {
                    EvaluationId = statusMessage.EvaluationId,
                    EventId = statusMessage.EventId.ToString(),
                    EventType = eventType,
                    EventValue = new Dictionary<string, object>
                    {
                        { Observability.EventParams.EvaluationId, statusMessage.EvaluationId },
                        { eventStatusParam, HBA1CPOCStatusCode.GetHBA1CPOCStatusCode(statusMessage.StatusCode).StatusCodeName }
                    }
                };
                if (additionalEventDetails is not null)
                {
                    foreach (var detail in additionalEventDetails)
                    {
                        observabilityEvent.EventValue.TryAdd(detail.Key, detail.Value);
                    }
                }

                _publishObservability.RegisterEvent(observabilityEvent, true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to add observability for EvaluationId={EvaluationId}, with StatusCodeId={StatusCodeId} and EventId={EventId}",
                statusMessage.EvaluationId, statusMessage.StatusCode, statusMessage.EventId);
        }
    }
}
