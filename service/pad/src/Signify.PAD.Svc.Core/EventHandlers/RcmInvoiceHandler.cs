using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Newtonsoft.Json;
using NServiceBus;
using Signify.PAD.Svc.Core.ApiClient;
using Signify.PAD.Svc.Core.ApiClient.Requests;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Constants;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.Core.Queries;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Refit;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Svc.Core.Exceptions;
using Signify.PAD.Svc.Core.Infrastructure;

namespace Signify.PAD.Svc.Core.EventHandlers
{
    public class RcmInvoiceHandler : IHandleMessages<RcmBillingRequest>
    {
        private readonly ILogger<RcmInvoiceHandler> _logger;
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly IRcmApi _rcmApi;
        private readonly ITransactionSupplier _transactionSupplier;
        private readonly IApplicationTime _applicationTime;
        private readonly IPublishObservability _publishObservability;


        public RcmInvoiceHandler(ILogger<RcmInvoiceHandler> logger,
            IRcmApi rcmApi,
            IMapper mapper,
            IMediator mediator,
            ITransactionSupplier transactionSupplier,
            IApplicationTime applicationTime,
            IPublishObservability publishObservability)
        {
            _logger = logger;
            _rcmApi = rcmApi;
            _mapper = mapper;
            _mediator = mediator;
            _transactionSupplier = transactionSupplier;
            _applicationTime = applicationTime;
            _publishObservability = publishObservability;
        }

        [Transaction]
        public async Task Handle(RcmBillingRequest message, IMessageHandlerContext context)
        {
            _logger.LogDebug("Received request for PadId={PadId}, EvaluationId={EvaluationId}", message.PadId, message.EvaluationId);

            if (await HasAlreadyCreatedBillingRequest(message, context.CancellationToken))
            {
                _logger.LogInformation("Billing request has already been created for PadId={PadId}, EvaluationId={EvaluationId}, not sending billing request to RCM", message.PadId, message.EvaluationId);
                return;
            }

            var createBillRequest = _mapper.Map<CreateBillRequest>(message);
            _logger.LogInformation("Sending billing request to RCM - {EvaluationId}", message.EvaluationId);
            
            using var transaction = _transactionSupplier.BeginTransaction();
            
            var rcmResponse = await _rcmApi.SendBillingRequest(createBillRequest);
            
            RegisterObservabilityEvent(message, Observability.ProviderPay.RcmBillingApiStatusCodeEvent, Observability.EventParams.StatusCode,
                rcmResponse?.StatusCode, true);

            if (rcmResponse != null && (rcmResponse.IsSuccessStatusCode || rcmResponse.StatusCode == HttpStatusCode.MovedPermanently))
            {
                await HandleSuccessful(message, context, rcmResponse, createBillRequest, transaction);
            }
            else
            {
                HandleFailure(message, rcmResponse);
            }
        }

        private async Task HandleSuccessful(RcmBillingRequest message, IMessageHandlerContext context, IApiResponse<Guid?> rcmResponse,
            CreateBillRequest createBillRequest, IBufferedTransaction transaction)
        {
            var billId = GetBillId(rcmResponse);

            if (!string.IsNullOrWhiteSpace(billId)) //ignore if rcm is not enabled yet
            {
                var pad = await _mediator.Send(new GetPAD { EvaluationId = message.EvaluationId }, context.CancellationToken);
                await _mediator.Send(new CreateOrUpdateRcmBilling
                {
                    RcmBilling = new PADRCMBilling
                    {
                        BillId = billId,
                        PADId = pad.PADId,
                        CreatedDateTime = _applicationTime.UtcNow()
                    }
                }, context.CancellationToken);

                var padStatus = await _mediator.Send(new CreatePadStatus { PadId = pad.PADId, StatusCode = PADStatusCode.BillRequestSent }, context.CancellationToken);

                var billRequestSent = GetBillRequestSent(createBillRequest, padStatus, billId, pad);

                // Mediator call to Publish BillRequestSent
                await _mediator.Send(billRequestSent, context.CancellationToken);

                await transaction.CommitAsync(context.CancellationToken);
                    
                RegisterObservabilityEvent(message, Observability.ProviderPay.ProviderPayOrBillingEvent, Observability.EventParams.Type,
                    Observability.EventParams.TypeRcmBilling);
                    
                //add observability for dps evaluation dashboard
                PublishSuccessObservabilityEvent(message, Observability.RcmBilling.BillRequestRaisedEvent, billId);
                _publishObservability.Commit();
            }
            else
                _logger.LogInformation("PAD rcmBilling success but ignored: {EvaluationId}", message.EvaluationId);
        }

        private void HandleFailure(RcmBillingRequest message, IApiResponse<Guid?> rcmResponse)
        {
            //add observability for dps evaluation dashboard
            PublishFailedObservabilityEvent(message, Observability.RcmBilling.BillRequestFailedEvent);

            if (rcmResponse.ReasonPhrase == "Conflict")
            {
                //Only log the conflict and donot retry, as per discussion with T-checks
                _logger.LogError("PAD rcmBilling conflict: {EvaluationId}", message.EvaluationId);
            }
            else
            {
                
                var errorMessage = "Unsuccessful HTTP status code returned";
                if (!string.IsNullOrEmpty(rcmResponse.Error?.Content))
                    errorMessage = $"{errorMessage}, with response: {rcmResponse.Error.Content}";
                
                // Raise for NSB retry
                throw new RcmBillingRequestException(message.EvaluationId, rcmResponse.StatusCode,
                    errorMessage, rcmResponse.Error);
            }
        }

        private static string GetBillId(IApiResponse<Guid?> rcmRs)
        {
            //When Response is 202 : cmRs.Content will be not  null and Error will have null value. 
            //When Response is 301(MovedPermanently) : cmRs.Content will be null and Error will have value. 
            var billId = rcmRs.Content == null && rcmRs.Error != null ? 
                JsonConvert.DeserializeObject(rcmRs.Error.Content ?? string.Empty)?.ToString() : 
                rcmRs.Content.ToString();
            return billId;
        }
        
        private BillRequestSent GetBillRequestSent(CreateBillRequest rcmBilling, PADStatus padStatus, string billId, Data.Entities.PAD pad)
        {
            var billRequestSent = _mapper.Map<BillRequestSent>(padStatus);
            billRequestSent.PdfDeliveryDate = rcmBilling.BillableDate;
            billRequestSent.BillId = billId;
            billRequestSent = _mapper.Map(pad, billRequestSent);
            return billRequestSent;
        }

        private Task<bool> HasAlreadyCreatedBillingRequest(RcmBillingRequest message, CancellationToken cancellationToken)
            => _mediator.Send(new QueryBillRequestSent(message.PadId), cancellationToken);

        private void RegisterObservabilityEvent(RcmBillingRequest request, string eventType, string eventParam, object eventParamValue, bool sendImmediate = false)
        {
            var observabilityEvent = new ObservabilityEvent
            {
                EvaluationId = request.EvaluationId,
                EventId = GetCorrelationGuidFromString(request.CorrelationId),
                EventType = eventType,
                EventValue = new Dictionary<string, object>
                {
                    { Observability.EventParams.EvaluationId, request.EvaluationId },
                    { eventParam, eventParamValue }
                }
            };

            _publishObservability.RegisterEvent(observabilityEvent, sendImmediate);
        }
    
        private void PublishFailedObservabilityEvent(RcmBillingRequest request, string eventType)
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
        
        private void PublishSuccessObservabilityEvent(RcmBillingRequest request, string eventType, string billId)
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
        
        private Guid GetCorrelationGuidFromString(string correlationId)
        {
            try
            {
                return new Guid(correlationId);
            }
            catch
            {
                _logger.LogError("Could not convert string: {correlationId} to GUID, returning empty GUID to Observability event.", correlationId); 
                return Guid.Empty;
            }
        }
        
    }
}