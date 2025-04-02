using AutoMapper;
using UacrNsbEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.uACR.Core.Constants;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Events.Status;
using Signify.uACR.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;

namespace Signify.uACR.Core.Commands;

public class UpdateExamStatus : IRequest
{
    public ExamStatusEvent ExamStatus { get; set; }
}

public class UpdateExamStatusHandler(
    ILogger<UpdateExamStatusHandler> logger,
    IMapper mapper,
    IMediator mediator,
    IPublishObservability publishObservability)
    : IRequestHandler<UpdateExamStatus>
{
    [Transaction]
    public async Task Handle(UpdateExamStatus request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Start {Handle} for EvaluationId={EvaluationId}, EventId={EventId}",
            nameof(UpdateExamStatusHandler), request.ExamStatus.EvaluationId, request.ExamStatus.EventId);

        var status = (await SaveStatusToDb(request.ExamStatus, cancellationToken)).Status;
        var eventToPublish = CreateKafkaStatusEvent(request.ExamStatus, status.CreatedDateTime);
        if (eventToPublish is not null)
        {
            await PublishStatusToKafka(request.ExamStatus.EventId, eventToPublish, cancellationToken);
        }

        PublishObservabilityEvents(request.ExamStatus);
        logger.LogDebug("End {Handle} for EvaluationId={EvaluationId}", nameof(UpdateExamStatusHandler),
            request.ExamStatus.EvaluationId);
    }

    /// <summary>
    /// Write status to database
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    [Trace]
    private async Task<AddExamStatusResponse> SaveStatusToDb(ExamStatusEvent message,
        CancellationToken cancellationToken)
    {
        var status = new AddExamStatus
        (
            eventId: message.EventId,
            evaluationId: message.EvaluationId,
            status: new ExamStatus
            {
                ExamStatusCodeId = message.StatusCode.ExamStatusCodeId,
                StatusDateTime =
                    message.StatusDateTime.ToUniversalTime(), // The event time could be in timezone other than UTC
                ExamId = message.ExamId
            },
            alwaysAddStatus: IsMultipleStatusAllowed(message.StatusCode)
        );
        return await mediator.Send(status, cancellationToken);
    }

    /// <summary>
    /// Determine if multiple status codes are allowed
    /// </summary>
    /// <param name="statusCode">StatusCode enum</param>
    /// <returns></returns>
    [Trace]
    private static bool IsMultipleStatusAllowed(ExamStatusCode statusCode)
        => statusCode.ExamStatusCodeId switch
        {
            (int) StatusCode.CdiPassedReceived or (int) StatusCode.CdiFailedWithPayReceived
                or (int) StatusCode.CdiFailedWithoutPayReceived or (int) StatusCode.ProviderPayableEventReceived
                or (int) StatusCode.ProviderNonPayableEventReceived => true,
            _ => false
        };

    /// <summary>
    /// Create the event type based on the status code.
    /// Returns null if no kafka event is to be published.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="createdDateTime"></param>
    /// <exception cref="NotImplementedException"></exception>
    /// <returns></returns>
    [Trace]
    private BaseStatusMessage CreateKafkaStatusEvent(ExamStatusEvent message, DateTimeOffset createdDateTime)
    {
        switch (message.StatusCode.ExamStatusCodeId)
        {
            case (int) StatusCode.ExamPerformed:
            case (int) StatusCode.ExamNotPerformed:
            case (int) StatusCode.BillableEventReceived:
            case (int) StatusCode.BillRequestSent:
            case (int) StatusCode.ClientPdfDelivered:
            case (int) StatusCode.LabResultsReceived:
            case (int) StatusCode.BillRequestNotSent:
            case (int) StatusCode.CdiPassedReceived:
            case (int) StatusCode.CdiFailedWithPayReceived:
            case (int) StatusCode.CdiFailedWithoutPayReceived:
            case (int) StatusCode.OrderRequested:
                return null;
            case (int) StatusCode.ProviderPayableEventReceived:
                var providerPayableEvent = mapper.Map<ProviderPayableEventReceived>(message);
                providerPayableEvent.CreatedDate = createdDateTime;
                return providerPayableEvent;
            case (int) StatusCode.ProviderNonPayableEventReceived:
                var providerNonPayableEventReceived = mapper.Map<ProviderNonPayableEventReceived>(message);
                providerNonPayableEventReceived.CreatedDate = createdDateTime;
                return providerNonPayableEventReceived;
            case (int) StatusCode.ProviderPayRequestSent:
                var providerPayRequestEvent = mapper.Map<ProviderPayRequestSent>(message);
                providerPayRequestEvent.CreatedDate = createdDateTime;
                return providerPayRequestEvent;
            default:
                throw new NotImplementedException($"Status code {message.StatusCode.StatusName} has not been handled");
        }
    }

    /// <summary>
    /// Publish the status event to Kafka
    /// </summary>
    /// <param name="eventId"></param>
    /// <param name="eventToPublish"></param>
    /// <param name="cancellationToken"></param>
    [Trace]
    private async Task PublishStatusToKafka(Guid eventId, BaseStatusMessage eventToPublish,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new PublishStatusUpdate(eventId, eventToPublish), cancellationToken);
    }

    /// <summary>
    /// Method to emit events to surface in observability dashboard.
    /// Events are just added here but are committed when the transaction is committed.
    /// </summary>
    /// <param name="statusMessage"></param>
    [Trace]
    private void PublishObservabilityEvents(ExamStatusEvent statusMessage)
    {
        switch (statusMessage.StatusCode.ExamStatusCodeId)
        {
            case (int) StatusCode.CdiPassedReceived:
            case (int) StatusCode.CdiFailedWithPayReceived:
            case (int) StatusCode.CdiFailedWithoutPayReceived:
                Publish(Observability.ProviderPay.CdiEvents, Observability.EventParams.CdiEvent);
                return;
            case (int) StatusCode.ProviderPayableEventReceived:
                Publish(Observability.ProviderPay.PayableCdiEvents, Observability.EventParams.PayableCdiEvents);
                return;
            case (int) StatusCode.ProviderNonPayableEventReceived:
                Publish(Observability.ProviderPay.NonPayableCdiEvents,
                    Observability.EventParams.NonPayableCdiEvents,
                    new Dictionary<string, object>
                    {
                        {Observability.EventParams.NonPayableReason, ((ProviderPayStatusEvent) statusMessage).Reason}
                    });
                return;
            case (int) StatusCode.ProviderPayRequestSent:
                Publish(Observability.ProviderPay.PayableCdiEvents,
                    Observability.EventParams.PayableCdiEvents,
                    new Dictionary<string, object>
                    {
                        {Observability.EventParams.PaymentId, ((ProviderPayStatusEvent) statusMessage).PaymentId}
                    });
                return;
            case (int)StatusCode.OrderRequested:
                Publish(Observability.OmsOrderCreation.OrderCreationEvents,
                    Observability.EventParams.OrderCreationEvents,
                    new Dictionary<string, object>
                    {
                        { Observability.EventParams.Barcode, ((OrderRequestedStatusEvent)statusMessage).Barcode },
                        { Observability.EventParams.Vendor, ((OrderRequestedStatusEvent)statusMessage).Vendor }
                    });
                return;
            case (int) StatusCode.ExamPerformed:
            case (int) StatusCode.ExamNotPerformed:
            case (int) StatusCode.BillableEventReceived:
            case (int) StatusCode.BillRequestSent:
            case (int) StatusCode.ClientPdfDelivered:
            case (int) StatusCode.LabResultsReceived:
            case (int) StatusCode.BillRequestNotSent:
            default:
                return;
        }

        void Publish(string eventType, string eventStatusParam,
            Dictionary<string, object> additionalEventDetails = null)
        {
            var observabilityEvent = new ObservabilityEvent
            {
                EvaluationId = statusMessage.EvaluationId,
                EventId = statusMessage.EventId,
                EventType = eventType,
                EventValue = new Dictionary<string, object>
                {
                    {Observability.EventParams.EvaluationId, statusMessage.EvaluationId},
                    {eventStatusParam, statusMessage.StatusCode.StatusName}
                }
            };
            if (additionalEventDetails is not null)
            {
                foreach (var detail in additionalEventDetails)
                {
                    observabilityEvent.EventValue.TryAdd(detail.Key, detail.Value);
                }
            }

            publishObservability.RegisterEvent(observabilityEvent);
        }
    }
}