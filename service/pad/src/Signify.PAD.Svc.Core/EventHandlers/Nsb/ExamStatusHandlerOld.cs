using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Constants;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.Core.Models;
using Signify.PAD.Svc.Core.Queries;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public class ExamStatusHandlerOld : IHandleMessages<ExamStatusEvent>
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly ITransactionSupplier _transactionSupplier;
    private readonly IObservabilityService _observabilityService;

    public ExamStatusHandlerOld(ILogger<ExamStatusHandlerOld> logger,
        IMapper mapper,
        IMediator mediator,
        ITransactionSupplier transactionSupplier,
        IObservabilityService observabilityService)
    {
        _logger = logger;
        _mapper = mapper;
        _mediator = mediator;
        _transactionSupplier = transactionSupplier;
        _observabilityService = observabilityService;
    }

    public async Task Handle(ExamStatusEvent message, IMessageHandlerContext context)
    {
        _logger.LogDebug("Start {Handle} for EvaluationId={EvaluationId}, PadId={PadId} and EventId={EventId}",
            nameof(ExamStatusHandlerOld), message.EvaluationId, message.ExamId, message.EventId);

        using var transaction = _transactionSupplier.BeginTransaction();
        var exam = await GetExam(message.EvaluationId, context.CancellationToken);
        await SaveStatusToDb(message, exam, context.CancellationToken);
        
        var eventToPublish = CreateKafkaStatusEvent(message.StatusCode, message, exam);
        if (eventToPublish is not null)
        {
            await PublishStatusToKafka(message.EventId, eventToPublish, context.CancellationToken);
        }

        await transaction.CommitAsync(context.CancellationToken);
        PublishObservabilityEvents(message);
        _logger.LogDebug("End {Handle} for EvaluationId={EvaluationId}", nameof(ExamStatusHandlerOld), message.EvaluationId);
    }

    /// <summary>
    /// Get the PAD exam details from database based on <see cref="evaluationId"/>
    /// </summary>
    /// <param name="evaluationId">EvaluationId of the corresponding PAD exam</param>
    /// <returns></returns>
    private Task<PAD> GetExam(long evaluationId, CancellationToken cancellationToken)
    {
        return _mediator.Send(new GetPAD { EvaluationId = evaluationId }, cancellationToken);
    }

    private Task SaveStatusToDb(ExamStatusEvent message, PAD exam, CancellationToken cancellationToken)
    {
        // In case ExamId (PADId, in this case) is not set by the caller
        if (message.ExamId < 1)
        {
            message.ExamId = exam.PADId;
        }

        return _mediator.Send(new CreatePadStatus { PadId = message.ExamId, StatusCode = message.StatusCode }, cancellationToken);
    }

    /// <summary>
    /// Create the event type based on the status code.
    /// Returns null if no kafka event is to be published.
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="message"></param>
    /// <param name="exam"></param>
    /// <returns></returns>
    private PadStatusCode CreateKafkaStatusEvent(PADStatusCode statusCode, ExamStatusEvent message, PAD exam)
    {
        switch (statusCode.PADStatusCodeId)
        {
            case (int)StatusCodes.CdiPassedReceived:
            case (int)StatusCodes.CdiFailedWithPayReceived:
            case (int)StatusCodes.CdiFailedWithoutPayReceived:
                return null;
            case (int)StatusCodes.ProviderPayableEventReceived:
                var providerPayableEvent = _mapper.Map<ProviderPayableEventReceived>(exam);
                providerPayableEvent.ParentCdiEvent = ((ProviderPayStatusEvent)message).ParentCdiEvent;
                return providerPayableEvent;
            case (int)StatusCodes.ProviderNonPayableEventReceived:
                var providerNonPayableEventReceived = _mapper.Map<ProviderNonPayableEventReceived>(exam);
                providerNonPayableEventReceived.ParentCdiEvent = ((ProviderPayStatusEvent)message).ParentCdiEvent;
                providerNonPayableEventReceived.Reason = ((ProviderPayStatusEvent)message).Reason;
                return providerNonPayableEventReceived;
            case (int)StatusCodes.ProviderPayRequestSent:
                var providerPayRequestEvent = _mapper.Map<ProviderPayRequestSent>(exam);
                providerPayRequestEvent.PaymentId = ((ProviderPayStatusEvent)message).PaymentId;
                providerPayRequestEvent.PdfDeliveryDate = message.StatusDateTime.UtcDateTime;
                return providerPayRequestEvent;
            default:
                return null;
        }
    }

    private Task PublishStatusToKafka(Guid eventId, PadStatusCode eventToPublish, CancellationToken cancellationToken)
    {
        return _mediator.Send(new PublishStatusUpdateOld(eventId, eventToPublish), cancellationToken);
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
            switch (statusMessage.StatusCode.PADStatusCodeId)
            {
                case (int)StatusCodes.CdiPassedReceived:
                case (int)StatusCodes.CdiFailedWithPayReceived:
                case (int)StatusCodes.CdiFailedWithoutPayReceived:
                    Publish(Observability.ProviderPay.CdiEvents, Observability.EventParams.CdiEvent);
                    break;
                case (int)StatusCodes.ProviderPayableEventReceived:
                    Publish(Observability.ProviderPay.PayableCdiEvents, Observability.EventParams.PayableCdiEvents);
                    break;
                case (int)StatusCodes.ProviderNonPayableEventReceived:
                    Publish(Observability.ProviderPay.NonPayableCdiEvents,
                        Observability.EventParams.NonPayableCdiEvents,
                        new Dictionary<string, object>
                        {
                            { Observability.EventParams.NonPayableReason, ((ProviderPayStatusEvent)statusMessage).Reason }
                        });
                    break;
                case (int)StatusCodes.ProviderPayRequestSent:
                    Publish(Observability.ProviderPay.PayableCdiEvents,
                        Observability.EventParams.PayableCdiEvents,
                        new Dictionary<string, object>
                        {
                            { Observability.EventParams.PaymentId, ((ProviderPayStatusEvent)statusMessage).PaymentId }
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
            _logger.LogError(ex,
                "Exception in {Handler} while adding observability for StatusCodeId={StatusCodeId} with EvaluationId={EvaluationId}, PadId={PadId}, EventId={EventId}",
                nameof(ExamStatusHandlerOld), statusMessage.StatusCode.PADStatusCodeId, statusMessage.EvaluationId, statusMessage.ExamId, statusMessage.EventId);
        }
    }
}