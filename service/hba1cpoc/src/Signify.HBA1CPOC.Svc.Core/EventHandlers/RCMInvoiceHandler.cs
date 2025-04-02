using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Newtonsoft.Json;
using NServiceBus;
using Signify.HBA1CPOC.Svc.Core.ApiClient;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Requests;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Data;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.Svc.Core.Queries;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Refit;
using Signify.HBA1CPOC.Messages.Events;
using Signify.HBA1CPOC.Messages.Events.Status;
using Signify.HBA1CPOC.Svc.Core.Constants;
using Signify.HBA1CPOC.Svc.Core.Exceptions;
using Signify.HBA1CPOC.Svc.Core.Infrastructure;
using Signify.HBA1CPOC.Svc.Core.Infrastructure.Observability;

namespace Signify.HBA1CPOC.Svc.Core.EventHandlers;

public class RCMInvoiceHandler : IHandleMessages<RCMBillingRequest>
{
    private readonly ILogger _logger;
    private readonly IApplicationTime _applicationTime;
    private readonly ITransactionSupplier _transactionSupplier;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly IRcmApi _rcmApi;
    private readonly IPublishObservability _publishObservability;

    public RCMInvoiceHandler(ILogger<RCMInvoiceHandler> logger,
        IApplicationTime applicationTime,
        ITransactionSupplier transactionSupplier,
        IRcmApi rcmApi,
        IMapper mapper,
        IMediator mediator,
        IPublishObservability publishObservability)
    {
        _logger = logger;
        _applicationTime = applicationTime;
        _transactionSupplier = transactionSupplier;
        _rcmApi = rcmApi;
        _mapper = mapper;
        _mediator = mediator;
        _publishObservability = publishObservability;
    }

    [Transaction]
    public async Task Handle(RCMBillingRequest message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Received request for EvaluationId={EvaluationId}", message.EvaluationId);

        var rcmBillingRecord = await _mediator.Send(new GetRcmBilling
        {
            HbA1cPocId = message.Hba1cpocId
        }, context.CancellationToken);

        if (rcmBillingRecord != null)
        {
            _logger.LogInformation("RCM billing request already exists for EvaluationId={EvaluationId}", message.EvaluationId);
            return;
        }

        var createBillRequest = _mapper.Map<CreateBillRequest>(message);

        var response = await _rcmApi.SendBillingRequest(createBillRequest);

        RegisterObservabilityEvent(message, Observability.ProviderPay.RcmBillingApiStatusCodeEvent, Observability.EventParams.StatusCode,
            response?.StatusCode, true);

        ValidateSuccessful(message, createBillRequest, response);

        var billId = GetBillId(message, response).ToString();

        PublishSuccessObservabilityEvent(message, Observability.RcmBilling.BillRequestRaisedEvent, billId);
        
        using var transaction = _transactionSupplier.BeginTransaction();

        var entity = await _mediator.Send(new GetHBA1CPOC { EvaluationId = message.EvaluationId }, context.CancellationToken);
        var status = await _mediator.Send(new CreateHBA1CPOCStatus
        {
            HBA1CPOCId = entity.HBA1CPOCId,
            StatusCodeId = HBA1CPOCStatusCode.BillRequestSent.HBA1CPOCStatusCodeId
        }, context.CancellationToken);

        await _mediator.Send(new CreateOrUpdateRCMBilling
        {
            RcmBilling = new HBA1CPOCRCMBilling
            {
                BillId = billId,
                HBA1CPOCId = entity.HBA1CPOCId,
                CreatedDateTime = _applicationTime.UtcNow()
            }
        }, context.CancellationToken);
        
        RegisterObservabilityEvent(message, Observability.ProviderPay.ProviderPayOrBillingEvent, Observability.EventParams.Type,
            Observability.EventParams.TypeRcmBilling);

        await PublishStatus(message.EventId, status, entity, createBillRequest, billId);

        await transaction.CommitAsync(context.CancellationToken);
        _publishObservability.Commit();
    }

    private void ValidateSuccessful(RCMBillingRequest message, CreateBillRequest request, IApiResponse response)
    {
        // See: https://wiki.signifyhealth.com/display/BILL/Integration+Guide

        // Response codes returned by API:
        // Accepted (202)
        // Moved Permanently (301)
        // Bad Request (400)

        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.MovedPermanently)
        {
            _logger.LogInformation("Received StatusCode={StatusCode} from RCM API, for EvaluationId={EvaluationId}", response.StatusCode, message.EvaluationId);
            return;
        }

        _logger.LogWarning("Received StatusCode={StatusCode} from RCM API for EvaluationId={EvaluationId} with Request={Request}", response.StatusCode, message.EvaluationId, JsonConvert.SerializeObject(request));

        var exMessage = "Unsuccessful HTTP status code returned";
        if (!string.IsNullOrEmpty(response.Error?.Content))
            exMessage = $"{exMessage}, with response: {response.Error.Content}"; // For 400, RCM includes the failure reason in the response content; see https://wiki.signifyhealth.com/display/BILL/Integration+Guide

        PublishFailedObservabilityEvent(message, Observability.RcmBilling.BillRequestFailedEvent);
        
        // Raise for NSB retry
        throw new RcmBillingRequestException(message.EventId, message.EvaluationId, response.StatusCode, exMessage, response.Error);
    }

    private static Guid GetBillId(RCMBillingRequest message, IApiResponse<Guid?> response)
    {
        // 200-level response codes will have the BillId guid in the content
        if (response.Content.HasValue)
        {
            return response.Content.Value;
        }

        // 300-level response codes will have the BillId guid in the error content
        if (response.Error?.Content != null)
        {
            var billId = JsonConvert.DeserializeObject<Guid>(response.Error.Content);
            if (billId != Guid.Empty)
                return billId;
        }

        // Raise for NSB retry
        throw new RcmBillingRequestException(message.EventId, message.EvaluationId, response.StatusCode,
            "BillId was not included in the API response");
    }

    private async Task PublishStatus(Guid eventId, HBA1CPOCStatus status, Data.Entities.HBA1CPOC entity, CreateBillRequest createBillRequest, string billId)
    {
        var billRequestSent = _mapper.Map<BillRequestSent>(status);
        billRequestSent = _mapper.Map(entity,billRequestSent);
        billRequestSent.PdfDeliveryDate = createBillRequest.BillableDate;
        billRequestSent.BillId = billId;
        billRequestSent.ReceivedDate = _applicationTime.UtcNow();

        await _mediator.Send(new PublishStatusUpdate(eventId, billRequestSent));
    }
    
    private void RegisterObservabilityEvent(RCMBillingRequest request, string eventType, string eventParam, object eventParamValue, bool sendImmediate = false)
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
    
    private void PublishFailedObservabilityEvent(RCMBillingRequest request, string eventType)
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
    private void PublishSuccessObservabilityEvent(RCMBillingRequest request, string eventType, string billId)
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
