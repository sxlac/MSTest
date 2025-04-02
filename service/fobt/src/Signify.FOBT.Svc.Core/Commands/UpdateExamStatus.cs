using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.FOBT.Messages.Events.Status;
using Signify.FOBT.Svc.Core.Constants;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Events.Status;
using Signify.FOBT.Svc.Core.Infrastructure.Observability;
using Signify.FOBT.Svc.Core.Models;
using Signify.FOBT.Svc.Core.Queries;

namespace Signify.FOBT.Svc.Core.Commands;

public class UpdateExamStatus: IRequest
{
    public ExamStatusEvent ExamStatus { get; set; }
}

public class UpdateExamStatusHandler : IRequestHandler<UpdateExamStatus>
{
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly ILogger<UpdateExamStatusHandler> _logger;
    private readonly IPublishObservability _publishObservabilityEvents;

    public UpdateExamStatusHandler(ILogger<UpdateExamStatusHandler> logger, IMapper mapper, IMediator mediator, IPublishObservability publishObservability)
    {
        _mapper = mapper;
        _mediator = mediator;
        _publishObservabilityEvents = publishObservability;
        _logger = logger;
    }

    [Transaction]
    public async Task Handle(UpdateExamStatus request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Start {Handle} for EvaluationId={EvaluationId}, EventId={EventId}",
            nameof(UpdateExamStatus), request.ExamStatus.EvaluationId, request.ExamStatus.EventId);
        var exam = request.ExamStatus.Exam ?? await GetExam(request.ExamStatus.EvaluationId, cancellationToken);
        var status = await SaveStatusToDb(request.ExamStatus, exam, cancellationToken);

        var eventToPublish = CreateKafkaStatusEvent(request.ExamStatus, exam, status.CreatedDateTime);
        if (eventToPublish is not null)
        {
            await PublishStatusToKafka(eventToPublish, cancellationToken);
        }

        PublishObservabilityEvents(request.ExamStatus);
        _logger.LogDebug("End {Handle} for EvaluationId={EvaluationId}", nameof(UpdateExamStatus), request.ExamStatus.EvaluationId);
        
    }

    /// <summary>
    /// Get the FOBT exam details from database based on <see cref="evaluationId"/>
    /// </summary>
    /// <param name="evaluationId">EvaluationId of the corresponding FOBT exam</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<Data.Entities.FOBT> GetExam(long evaluationId, CancellationToken cancellationToken)
    {
        return await _mediator.Send(new GetFOBT { EvaluationId = evaluationId }, cancellationToken);
    }
    
    private async Task<FOBTStatus> SaveStatusToDb(ExamStatusEvent message, Data.Entities.FOBT exam,
        CancellationToken cancellationToken)
    {
        return await _mediator.Send(new CreateFOBTStatus
            {
                FOBT = exam, StatusCode = message.StatusCode
            },
            cancellationToken);
    }

    /// <summary>
    /// Create the event type based on the status code.
    /// Returns null if no kafka event is to be published.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="exam"></param>
    /// <param name="createdDateTime"></param>
    /// <exception cref="NotImplementedException"></exception>
    /// <returns></returns>
    private BaseStatusMessage CreateKafkaStatusEvent(ExamStatusEvent message, Data.Entities.FOBT exam, DateTimeOffset createdDateTime)
    {
        switch (message.StatusCode.FOBTStatusCodeId)
        {
            case (int)FOBTStatusCode.StatusCodes.FobtPerformed:
            case (int)FOBTStatusCode.StatusCodes.InventoryUpdateRequested:
            case (int)FOBTStatusCode.StatusCodes.InventoryUpdateSuccess:
            case (int)FOBTStatusCode.StatusCodes.InventoryUpdateFail:
            case (int)FOBTStatusCode.StatusCodes.BillRequestSent:
            case (int)FOBTStatusCode.StatusCodes.OrderUpdated:
            case (int)FOBTStatusCode.StatusCodes.ValidLabResultsReceived:
            case (int)FOBTStatusCode.StatusCodes.LabOrderCreated:
            case (int)FOBTStatusCode.StatusCodes.FobtNotPerformed:
            case (int)FOBTStatusCode.StatusCodes.InvalidLabResultsReceived:
            case (int)FOBTStatusCode.StatusCodes.ClientPdfDelivered:
            case (int)FOBTStatusCode.StatusCodes.LeftBehindBillRequestSent:
            case (int)FOBTStatusCode.StatusCodes.ResultsBillRequestSent:
            case (int)FOBTStatusCode.StatusCodes.BillRequestNotSent:
            case (int)FOBTStatusCode.StatusCodes.CdiPassedReceived:
            case (int)FOBTStatusCode.StatusCodes.CdiFailedWithPayReceived:
            case (int)FOBTStatusCode.StatusCodes.CdiFailedWithoutPayReceived:
            case (int)FOBTStatusCode.StatusCodes.OrderHeld:
                return null;
            case (int)FOBTStatusCode.StatusCodes.ProviderPayableEventReceived:
                var providerPayableEvent = _mapper.Map<ProviderPayableEventReceived>(exam);
                providerPayableEvent.CreatedDate = createdDateTime;
                providerPayableEvent.ReceivedDate = message.ParentEventReceivedDateTime;
                providerPayableEvent.ParentCdiEvent = ((ProviderPayStatusEvent)message).ParentCdiEvent;
                return providerPayableEvent;
            case (int)FOBTStatusCode.StatusCodes.ProviderNonPayableEventReceived:
                var providerNonPayableEventReceived = _mapper.Map<ProviderNonPayableEventReceived>(exam);
                providerNonPayableEventReceived.CreatedDate = createdDateTime;
                providerNonPayableEventReceived.ReceivedDate = message.ParentEventReceivedDateTime;
                providerNonPayableEventReceived.ParentCdiEvent = ((ProviderPayStatusEvent)message).ParentCdiEvent;
                providerNonPayableEventReceived.Reason = ((ProviderPayStatusEvent)message).Reason;
                return providerNonPayableEventReceived;
            case (int)FOBTStatusCode.StatusCodes.ProviderPayRequestSent:
                var providerPayRequestEvent = _mapper.Map<ProviderPayRequestSent>(exam);
                providerPayRequestEvent.CreatedDate = createdDateTime;
                providerPayRequestEvent.ReceivedDate = message.ParentEventReceivedDateTime;
                providerPayRequestEvent.PaymentId = ((ProviderPayStatusEvent)message).PaymentId;
                providerPayRequestEvent.ParentEventDateTime =  message.StatusDateTime;
                return providerPayRequestEvent;
            default:
                    throw new NotImplementedException($"Status code {message.StatusCode.StatusCode} has not been handled");
        }
    }

    private async Task PublishStatusToKafka(BaseStatusMessage eventToPublish, CancellationToken cancellationToken)
    {
        await _mediator.Send(new PublishStatusUpdate(eventToPublish), cancellationToken);
    }

    /// <summary>
    /// Method to emit events to surface in observability dashboard.
    /// Events are just added here but are committed when the transaction is committed.
    /// </summary>
    /// <param name="statusMessage"></param>
    [Trace]
    private void PublishObservabilityEvents(ExamStatusEvent statusMessage)
    {
        switch (statusMessage.StatusCode.FOBTStatusCodeId)
        {
            case (int)FOBTStatusCode.StatusCodes.CdiPassedReceived:
            case (int)FOBTStatusCode.StatusCodes.CdiFailedWithPayReceived:
            case (int)FOBTStatusCode.StatusCodes.CdiFailedWithoutPayReceived:
                Publish(Observability.ProviderPay.CdiEvents, Observability.EventParams.CdiEvent);
                return;
            case (int)FOBTStatusCode.StatusCodes.ProviderPayableEventReceived:
                Publish(Observability.ProviderPay.PayableCdiEvents, Observability.EventParams.PayableCdiEvents);
                return;
            case (int)FOBTStatusCode.StatusCodes.ProviderNonPayableEventReceived:
                Publish(Observability.ProviderPay.NonPayableCdiEvents,
                    Observability.EventParams.NonPayableCdiEvents,
                    new Dictionary<string, object>
                    {
                        { Observability.EventParams.NonPayableReason, ((ProviderPayStatusEvent)statusMessage).Reason }
                    });
                return;
            case (int)FOBTStatusCode.StatusCodes.ProviderPayRequestSent:
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
                EventId = statusMessage.EventId.ToString(),
                EventType = eventType,
                EventValue = new Dictionary<string, object>
                {
                    { Observability.EventParams.EvaluationId, statusMessage.EvaluationId },
                    { eventStatusParam, statusMessage.StatusCode.StatusCode }
                }
            };

            if (additionalEventDetails is not null)
            {
                foreach (var detail in additionalEventDetails)
                {
                    observabilityEvent.EventValue.TryAdd(detail.Key, detail.Value);
                }
            }

            _publishObservabilityEvents.RegisterEvent(observabilityEvent);
        }
    }
}