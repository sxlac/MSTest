using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Newtonsoft.Json;
using NServiceBus;
using Signify.FOBT.Messages.Events.Status;
using Signify.FOBT.Svc.Core.ApiClient.Requests;
using Signify.FOBT.Svc.Core.ApiClient;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Constants;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Infrastructure.Observability;
using Signify.FOBT.Svc.Core.Queries;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System;
using Signify.FOBT.Svc.Core.Exceptions;

namespace Signify.FOBT.Svc.Core.EventHandlers;

public class RcmRequestEventHandler : IHandleMessages<RCMRequestEvent>
{
    private readonly ILogger<RcmRequestEventHandler> _logger;
    private readonly FOBTDataContext _dataContext;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly IRcmApi _rcmApi;
    private readonly IPublishObservability _publishObservability;

    public RcmRequestEventHandler(ILogger<RcmRequestEventHandler> logger, 
        FOBTDataContext dataContext, 
        IMediator mediator, 
        IMapper mapper, 
        IRcmApi rcmApi,
        IPublishObservability publishObservability)
    {
        _logger = logger;
        _dataContext = dataContext;
        _mediator = mediator;
        _mapper = mapper;
        _rcmApi = rcmApi;
        _publishObservability = publishObservability;
    }

    [Transaction]
    public async Task Handle(RCMRequestEvent request, IMessageHandlerContext context)
    {
        _logger.LogInformation("Start Handle RCMInvoiceHandler, EvaluationID:{EvaluationId}, ExamId:{FobtId}", request.EvaluationId, request.FOBTId);

        var rcmBillingRecord = await _mediator.Send(new GetFobtBilling(request.FOBT.FOBTId, request.BillingProductCode), context.CancellationToken);
        if (rcmBillingRecord != null)
        {
            _logger.LogInformation("We already recorded a bill for EvaluationID:{EvaluationId} and FOBT Id:{FOBTId} and BillingProductCode:{BillingProductCode} and will not attempt to send another bill", request.EvaluationId, request.FOBT.FOBTId, request.BillingProductCode);
            return;
        }

        var rcmBilling = _mapper.Map<RCMBilling>(request);

        _logger.LogInformation("Sending FOBT rcmBilling: {EvaluationId}", request.EvaluationId);

        var rcmResponse = await _rcmApi.SendRcmRequestForBilling(rcmBilling);
            
        RegisterObservabilityEvent(request, Observability.ProviderPay.RcmBillingApiStatusCodeEvent, Observability.EventParams.StatusCode,
            rcmResponse?.StatusCode, true);
        RegisterObservabilityEvent(request, Observability.ProviderPay.ProviderPayOrBillingEvent, Observability.EventParams.Type,
            Observability.EventParams.TypeRcmBilling);

        if (rcmResponse.IsSuccessStatusCode || rcmResponse.StatusCode == HttpStatusCode.MovedPermanently)
        {
            // When Response is 202 : cmRs.Content will be not null and Error will have null value.
            // When Response is 301 (MovedPermanently) : cmRs.Content will be null and Error will have value.
            var rcmBillId = rcmResponse.Content is null && rcmResponse.Error != null ?
                JsonConvert.DeserializeObject(rcmResponse.Error.Content).ToString() :
                rcmResponse.Content.ToString();

            if (!string.IsNullOrWhiteSpace(rcmBillId))
            {
                await using var transaction = await _dataContext.Database.BeginTransactionAsync(context.CancellationToken);
                await _mediator.Send(new CreateOrUpdateRcmBilling
                {
                    RcmBilling = new FOBTBilling
                    {
                        BillId = rcmBillId,
                        FOBTId = request.FOBTId,
                        CreatedDateTime = DateTimeOffset.UtcNow,
                        BillingProductCode = request.BillingProductCode
                    }
                }, context.CancellationToken);

                await _mediator.Send(new CreateFOBTStatus()
                {
                    FOBT = request.FOBT,
                    StatusCode = FOBTStatusCode.GetFOBTStatusCode(request.StatusCode)
                }, context.CancellationToken);

                await CallBillRequestSentHandler(request, rcmBilling, rcmBillId);
                await transaction.CommitAsync(context.CancellationToken);
                _publishObservability.Commit();

                _logger.LogInformation("FOBT rcmBilling success: {EvaluationId}", request.EvaluationId);
                    
                PublishSuccessObservabilityEvent(request, Observability.RcmBilling.BillRequestRaisedEvent, rcmBillId);
            }
            else
            {
                _logger.LogError("FOBT rcmBilling success but ignored: {EvaluationId}", request.EvaluationId);
            }

            return;
        }

        if (!string.Equals(rcmResponse.ReasonPhrase, "Conflict", StringComparison.OrdinalIgnoreCase))
        {
            PublishFailedObservabilityEvent(request, Observability.RcmBilling.BillRequestFailedEvent);
            throw new RcmBillingException(request.EvaluationId,  rcmResponse.StatusCode, $"There is an error in RCM", rcmResponse.Error);
        }

        //Only log the conflict and do not retry, as per discussion with T-checks
        _logger.LogError("FOBT rcmBilling conflict: {EvaluationId}", request.EvaluationId);
    }

    private async Task CallBillRequestSentHandler(RCMRequestEvent request, RCMBilling rcmBilling, string billId)
    {
        var publishBillRequestSent = _mapper.Map<BillRequestSent>(request.FOBT);
        publishBillRequestSent.PdfDeliveryDate = rcmBilling.BillableDate;
        publishBillRequestSent.BillId = billId;
        publishBillRequestSent.BillingProductCode = request.StatusCode;

        await _mediator.Send(new PublishStatusUpdate(publishBillRequestSent));
    }
        
    private void RegisterObservabilityEvent(RCMRequestEvent request, string eventType, string eventParam, object eventParamValue, bool sendImmediate = false)
    {
        var observabilityEvent = new ObservabilityEvent
        {
            EvaluationId = request.EvaluationId,
            EventId = request.CorrelationId,
            EventType = eventType,
            EventValue = new Dictionary<string, object>
            {
                { Observability.EventParams.EvaluationId, request.EvaluationId },
                { eventParam, eventParamValue }
            }
        };

        _publishObservability.RegisterEvent(observabilityEvent, sendImmediate);
    }
    private void PublishFailedObservabilityEvent(RCMRequestEvent request, string eventType)
    {
        var observabilityBillRequestFailedEvent = new ObservabilityEvent
        {
            EvaluationId = request.EvaluationId,
            EventType = eventType,
            EventValue = new Dictionary<string, object>
            {
                {Observability.EventParams.EvaluationId, request.EvaluationId},
                {Observability.EventParams.CreatedDateTime, ((DateTimeOffset)request.BillableDate).ToUnixTimeSeconds()}
            }
        };

        _publishObservability.RegisterEvent(observabilityBillRequestFailedEvent, true);
    }
    private void PublishSuccessObservabilityEvent(RCMRequestEvent request, string eventType, string billId)
    {
        var observabilityBillRequestRaisedEvent = new ObservabilityEvent
        {
            EvaluationId = request.EvaluationId,
            EventType = eventType,
            EventValue = new Dictionary<string, object>
            {
                {Observability.EventParams.EvaluationId, request.EvaluationId},
                {Observability.EventParams.CreatedDateTime, ((DateTimeOffset)request.BillableDate).ToUnixTimeSeconds()},
                {Observability.EventParams.BillId, billId}
            }
        };

        _publishObservability.RegisterEvent(observabilityBillRequestRaisedEvent, true);
    }
}