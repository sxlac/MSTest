using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.Constants;
using Signify.CKD.Svc.Core.Data;
using Signify.CKD.Svc.Core.Data.Entities;
using Signify.CKD.Svc.Core.Events;
using Signify.CKD.Svc.Core.Infrastructure.Observability;
using Signify.CKD.Svc.Core.Messages.Status;
using Signify.CKD.Svc.Core.Models;
using Signify.CKD.Svc.Core.Queries;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public class ExamStatusHandler : IHandleMessages<ExamStatusEvent>
{
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly ITransactionSupplier _transactionSupplier;
    private readonly ILogger _logger;
    private readonly IObservabilityService _observabilityService;

    public ExamStatusHandler(ILogger<ExamStatusHandler> logger, IMapper mapper, IMediator mediator,
        ITransactionSupplier transactionSupplier, IObservabilityService observabilityService)
    {
        _mapper = mapper;
        _mediator = mediator;
        _transactionSupplier = transactionSupplier;
        _logger = logger;
        _observabilityService = observabilityService;
    }

    [Transaction]
    public async Task Handle(ExamStatusEvent message, IMessageHandlerContext context)
    {
        _logger.LogDebug("Start handle ExamStatusHandler for EvaluationId={EvaluationId} and EventId={EventId}",
            message.EvaluationId, message.EventId);

        using var transaction = _transactionSupplier.BeginTransaction();
        var exam = await GetExam(message.EvaluationId);
        await SaveStatusToDb(message, exam);
        var eventToPublish = CreateKafkaStatusEvent(message.StatusCode, message, exam);
        if (eventToPublish is not null)
        {
            await _mediator.Send(new PublishStatusUpdate(eventToPublish));
        }

        await transaction.CommitAsync();
        PublishObservabilityEvents(message);
        _logger.LogDebug("End handle ExamStatusHandler for EvaluationId={EvaluationId}", message.EvaluationId);
    }

    /// <summary>
    /// Get the CKD exam details from database based on <see cref="evaluationId"/>
    /// </summary>
    /// <param name="evaluationId">EvaluationId of the corresponding CKD exam</param>
    /// <returns></returns>
    private Task<CKD> GetExam(long evaluationId)
    {
        return _mediator.Send(new GetCKD { EvaluationId = evaluationId }, CancellationToken.None);
    }

    private Task SaveStatusToDb(ExamStatusEvent message, CKD exam)
    {
        return _mediator.Send(new CreateCKDStatus { CKDId = exam.CKDId, StatusCodeId = message.StatusCode.CKDStatusCodeId }, CancellationToken.None);
    }

    /// <summary>
    /// Create the event type based on the status code.
    /// Returns null if no kafka event is to be published.
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="message"></param>
    /// <param name="exam"></param>
    /// <returns></returns>
    private BaseStatusMessage CreateKafkaStatusEvent(CKDStatusCode statusCode, ExamStatusEvent message, CKD exam)
    {
        switch (statusCode.CKDStatusCodeId)
        {
            case (int)StatusCodes.ProviderPayableEventReceived:
                var providerPayableEvent = _mapper.Map<ProviderPayableEventReceived>(exam);
                providerPayableEvent.ParentCdiEvent = ((ProviderPayStatusEvent)message).ParentCdiEvent;
                return providerPayableEvent;
            case (int)StatusCodes.ProviderPayRequestSent:
                var providerPayRequestEvent = _mapper.Map<ProviderPayRequestSent>(exam);
                providerPayRequestEvent.PaymentId = ((ProviderPayStatusEvent)message).PaymentId;
                providerPayRequestEvent.PdfDeliveryDate = message.StatusDateTime.UtcDateTime;
                return providerPayRequestEvent;
            case (int)StatusCodes.CdiPassedReceived:
            case (int)StatusCodes.CdiFailedWithPayReceived:
            case (int)StatusCodes.CdiFailedWithoutPayReceived:
                return null;
            case (int)StatusCodes.ProviderNonPayableEventReceived:
                var providerNonPayableEventReceived = _mapper.Map<ProviderNonPayableEventReceived>(exam);
                providerNonPayableEventReceived.ParentCdiEvent = ((ProviderPayStatusEvent)message).ParentCdiEvent;
                providerNonPayableEventReceived.Reason = ((ProviderPayStatusEvent)message).Reason;
                return providerNonPayableEventReceived;
            default:
                return null;
        }
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
            switch (statusMessage.StatusCode.CKDStatusCodeId)
            {
                case (int)StatusCodes.ProviderPayableEventReceived:
                    Publish(Observability.ProviderPay.PayableCdiEvents, Observability.EventParams.PayableCdiEvents);
                    break;
                case (int)StatusCodes.CdiPassedReceived:
                case (int)StatusCodes.CdiFailedWithPayReceived:
                case (int)StatusCodes.CdiFailedWithoutPayReceived:
                    Publish(Observability.ProviderPay.CdiEvents, Observability.EventParams.CdiEvent);
                    break;
                case (int)StatusCodes.ProviderNonPayableEventReceived:
                    Publish(Observability.ProviderPay.NonPayableCdiEvents, Observability.EventParams.NonPayableCdiEvents, 
                        new Dictionary<string, object>
                    {
                        { Observability.EventParams.NonPayableReason, ((ProviderPayStatusEvent)statusMessage).Reason }
                    });
                    break;
            }

            void Publish(string eventType, string eventStatusParam, Dictionary<string, object> additionalEventDetails = null)
            {
                var eventBody = new Dictionary<string, object>
                {
                    { Observability.EventParams.EvaluationId, statusMessage.EvaluationId },
                    { eventStatusParam, statusMessage.StatusCode.StatusCode }
                };

                if (additionalEventDetails is not null)
                {
                    foreach (var detail in additionalEventDetails)
                    {
                        eventBody.TryAdd(detail.Key, detail.Value);
                    }
                }

                _observabilityService.AddEvent(eventType, eventBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to add observability for EvaluationId={EvaluationId}, with StatusCodeId={StatusCodeId} and EventId={EventId}",
                statusMessage.EvaluationId, statusMessage.StatusCode.CKDStatusCodeId, statusMessage.EventId);
        }
    }
}
