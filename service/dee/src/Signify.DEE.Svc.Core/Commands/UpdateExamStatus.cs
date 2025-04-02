using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.DEE.Messages.Status;
using Signify.DEE.Svc.Core.Constants;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Events.Status;
using Signify.DEE.Svc.Core.Messages.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;

namespace Signify.DEE.Svc.Core.Commands;

[ExcludeFromCodeCoverage]
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
        var status = (await SaveStatusToDb(request.ExamStatus, cancellationToken)).ExamStatus;
        var eventToPublish = CreateKafkaStatusEvent(request.ExamStatus, status.CreatedDateTime);
        if (eventToPublish is not null)
        {
            await PublishStatusToKafka(eventToPublish, cancellationToken);
        }

        PublishObservabilityEvents(request.ExamStatus);
        logger.LogDebug("End {Handle} for EvaluationId={EvaluationId}", nameof(UpdateExamStatusHandler), request.ExamStatus.EvaluationId);
    }

    /// <summary>
    /// Write status to database
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    private async Task<CreateStatusResponse> SaveStatusToDb(ExamStatusEvent message, CancellationToken cancellationToken)
    {
        var alwaysAddStatus = IsMultipleStatusAllowed(message.StatusCode);
        return await mediator.Send(new CreateStatus
            {
                ExamId = message.ExamId,
                ExamStatusCode = message.StatusCode,
                MessageDateTime = message.ParentEventReceivedDateTime.UtcDateTime,
                AlwaysAddStatus = alwaysAddStatus
            },
            cancellationToken);
    }

    /// <summary>
    /// Determine if multiple status codes are allowed
    /// </summary>
    /// <param name="statusCode"></param>
    /// <returns></returns>
    private static bool IsMultipleStatusAllowed(ExamStatusCode statusCode)
    {
        return statusCode.ExamStatusCodeId switch
        {
            (int) ExamStatusCode.StatusCodes.CdiPassedReceived
                or (int) ExamStatusCode.StatusCodes.CdiFailedWithPayReceived
                or (int) ExamStatusCode.StatusCodes.CdiFailedWithoutPayReceived
                or (int) ExamStatusCode.StatusCodes.ProviderPayableEventReceived
                or (int) ExamStatusCode.StatusCodes.ProviderNonPayableEventReceived => true,
            _ => false
        };
    }

    /// <summary>
    /// Create the event type based on the status code.
    /// Returns null if no kafka event is to be published.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="createdDateTime"></param>
    /// <exception cref="NotImplementedException"></exception>
    /// <returns></returns>
    private BaseStatusMessage CreateKafkaStatusEvent(ExamStatusEvent message, DateTimeOffset createdDateTime)
    {
        switch (message.StatusCode.ExamStatusCodeId)
        {
            case (int)ExamStatusCode.StatusCodes.ExamCreated:
            case (int)ExamStatusCode.StatusCodes.AwaitingInterpreation:
            case (int)ExamStatusCode.StatusCodes.Interpreted:
            case (int)ExamStatusCode.StatusCodes.ResultDataDownloaded:
            case (int)ExamStatusCode.StatusCodes.PdfDataDownloaded:
            case (int)ExamStatusCode.StatusCodes.SentToBilling:
            case (int)ExamStatusCode.StatusCodes.NoDeeImagesTaken:
            case (int)ExamStatusCode.StatusCodes.IrisImageReceived:
            case (int)ExamStatusCode.StatusCodes.Gradable:
            case (int)ExamStatusCode.StatusCodes.NotGradable:
            case (int)ExamStatusCode.StatusCodes.DeeImagesFound:
            case (int)ExamStatusCode.StatusCodes.IrisExamCreated:
            case (int)ExamStatusCode.StatusCodes.IrisResultDownloaded:
            case (int)ExamStatusCode.StatusCodes.PcpLetterSent:
            case (int)ExamStatusCode.StatusCodes.NoPcpFound:
            case (int)ExamStatusCode.StatusCodes.MemberLetterSent:
            case (int)ExamStatusCode.StatusCodes.SentToProviderPay:
            case (int)ExamStatusCode.StatusCodes.Performed:
            case (int)ExamStatusCode.StatusCodes.NotPerformed:
            case (int)ExamStatusCode.StatusCodes.BillableEventRecieved:
            case (int)ExamStatusCode.StatusCodes.Incomplete:
            case (int)ExamStatusCode.StatusCodes.BillRequestNotSent:
            case (int)ExamStatusCode.StatusCodes.CdiPassedReceived:
            case (int)ExamStatusCode.StatusCodes.CdiFailedWithPayReceived:
            case (int)ExamStatusCode.StatusCodes.CdiFailedWithoutPayReceived:
            case (int)ExamStatusCode.StatusCodes.IrisOrderSubmitted:
            case (int)ExamStatusCode.StatusCodes.IrisImagesSubmitted:
                return null;
            case (int)ExamStatusCode.StatusCodes.ProviderPayableEventReceived:
                var providerPayableEvent = mapper.Map<ProviderPayableEventReceived>(message);
                providerPayableEvent.CreateDate = createdDateTime;
                return providerPayableEvent;
            case (int)ExamStatusCode.StatusCodes.ProviderNonPayableEventReceived:
                var providerNonPayableEventReceived = mapper.Map<ProviderNonPayableEventReceived>(message);
                providerNonPayableEventReceived.CreateDate = createdDateTime;
                return providerNonPayableEventReceived;
            case (int)ExamStatusCode.StatusCodes.ProviderPayRequestSent:
                var providerPayRequestEvent = mapper.Map<ProviderPayRequestSent>(message);
                providerPayRequestEvent.CreateDate = createdDateTime;
                return providerPayRequestEvent;
            default:
                throw new NotImplementedException($"Status code {message.StatusCode.Name} has not been handled");
        }
    }

    private async Task PublishStatusToKafka(BaseStatusMessage eventToPublish, CancellationToken cancellationToken)
    {
        await mediator.Send(new PublishStatusUpdate(eventToPublish), cancellationToken);
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
            case (int)ExamStatusCode.StatusCodes.CdiPassedReceived:
            case (int)ExamStatusCode.StatusCodes.CdiFailedWithPayReceived:
            case (int)ExamStatusCode.StatusCodes.CdiFailedWithoutPayReceived:
                Publish(Observability.ProviderPay.CdiEvents, Observability.EventParams.CdiEvent);
                return;
            case (int)ExamStatusCode.StatusCodes.ProviderPayableEventReceived:
                Publish(Observability.ProviderPay.PayableCdiEvents, Observability.EventParams.PayableCdiEvents);
                return;
            case (int)ExamStatusCode.StatusCodes.ProviderNonPayableEventReceived:
                Publish(Observability.ProviderPay.NonPayableCdiEvents,
                    Observability.EventParams.NonPayableCdiEvents,
                    new Dictionary<string, object>
                    {
                        { Observability.EventParams.NonPayableReason, ((ProviderPayStatusEvent)statusMessage).Reason }
                    });
                return;
            case (int)ExamStatusCode.StatusCodes.ProviderPayRequestSent:
                Publish(Observability.ProviderPay.PayableCdiEvents,
                    Observability.EventParams.PayableCdiEvents,
                    new Dictionary<string, object>
                    {
                        { Observability.EventParams.PaymentId, ((ProviderPayStatusEvent)statusMessage).PaymentId }
                    });
                return;            
            default:
                return;
        }

        void Publish(string eventType, string eventStatusParam, Dictionary<string, object> additionalEventDetails = null)
        {
            var observabilityEvent = new ObservabilityEvent
            {
                EvaluationId = statusMessage.EvaluationId,
                EventId = statusMessage.EventId,
                EventType = eventType,
                EventValue = new Dictionary<string, object>
                {
                    { Observability.EventParams.EvaluationId, statusMessage.EvaluationId },
                    { eventStatusParam, statusMessage.StatusCode.Name }
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