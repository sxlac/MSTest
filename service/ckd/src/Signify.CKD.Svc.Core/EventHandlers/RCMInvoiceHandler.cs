using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Newtonsoft.Json;
using NServiceBus;
using Refit;
using Signify.CKD.Messages.Events;
using Signify.CKD.Svc.Core.ApiClient;
using Signify.CKD.Svc.Core.ApiClient.Requests;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.Constants;
using Signify.CKD.Svc.Core.Data;
using Signify.CKD.Svc.Core.Data.Entities;
using Signify.CKD.Svc.Core.Exceptions;
using Signify.CKD.Svc.Core.Infrastructure.Observability;
using Signify.CKD.Svc.Core.Messages.Status;
using Signify.CKD.Svc.Core.Queries;

namespace Signify.CKD.Svc.Core.EventHandlers
{
    public class RCMInvoiceHandler : IHandleMessages<RCMBillingRequest>
    {
        private readonly ILogger _logger;
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly ITransactionSupplier _transactionSupplier;
        private readonly IRcmApi _rcmApi;
        private readonly IObservabilityService _observabilityService;
        
        public RCMInvoiceHandler(ILogger<RCMInvoiceHandler> logger,
            IRcmApi rcmApi,
            IMapper mapper,
            ITransactionSupplier transactionSupplier,
            IMediator mediator,
            IObservabilityService observabilityService)
        {
            _logger = logger;
            _rcmApi = rcmApi;
            _mapper = mapper;
            _transactionSupplier = transactionSupplier;
            _mediator = mediator;
            _observabilityService = observabilityService;
        }

        [Transaction]
        public async Task Handle(RCMBillingRequest message, IMessageHandlerContext context)
        {
            _logger.LogDebug("Received request for EvaluationId={EvaluationId}", message.EvaluationId);

            var rcmBilling = await _mediator.Send(new GetRcmBilling { CKDId = message.CKDId });
            if (rcmBilling != null)
            {
                _logger.LogInformation("RCM billing request already exists for CkdId={CkdId}, EvaluationId={EvaluationId}", message.CKDId, message.EvaluationId);
                return;
            }

            var createBillRequest = _mapper.Map<RCMBilling>(message);

            var response = await _rcmApi.SendBillingRequest(createBillRequest);

            _observabilityService.AddEvent(Observability.ProviderPay.RcmBillingApiStatusCodeEvent, new Dictionary<string, object>
            {
                {Observability.EventParams.EvaluationId, message.EvaluationId},
                {Observability.EventParams.StatusCode, response?.StatusCode}
            });

            ValidateSuccessful(message, createBillRequest, response);
            
            var billId = GetBillId(message.EvaluationId, response).ToString();

            using var transaction = _transactionSupplier.BeginTransaction();

            var ckd = await _mediator.Send(new GetCKD { EvaluationId = message.EvaluationId });
            await _mediator.Send(new CreateRCMBilling { RcmBilling = new CKDRCMBilling { BillId = billId, CKDId = ckd.CKDId, CreatedDateTime = DateTimeOffset.UtcNow } });
            await _mediator.Send(new CreateCKDStatus { CKDId = ckd.CKDId, StatusCodeId = CKDStatusCode.BillRequestSent.CKDStatusCodeId });

            // Publish to Kafka
            var kafkaBillRequestSentMessage = _mapper.Map<BillRequestSent>(ckd);
            kafkaBillRequestSentMessage.PdfDeliveryDate = message.PdfDeliveryDateTime;
            kafkaBillRequestSentMessage.BillId = billId;
            await _mediator.Send(new PublishStatusUpdate(kafkaBillRequestSentMessage));
            
            await transaction.CommitAsync();
            
            _observabilityService.AddEvent(Observability.ProviderPay.ProviderPayOrBillingEvent, new Dictionary<string, object>
            {
                {Observability.EventParams.EvaluationId, message.EvaluationId},
                {Observability.EventParams.Type, "RcmBilling"}
            });

            _observabilityService.AddEvent(Observability.RcmBilling.BillRequestRaisedEvent, new Dictionary<string, object>
            {
                {Observability.EventParams.EvaluationId, message.EvaluationId},
                {Observability.EventParams.CreatedDateTime, ((DateTimeOffset)message.BillableDate).ToUnixTimeSeconds()},
                {Observability.EventParams.BillId, billId}
            });
        }

        private void ValidateSuccessful(RCMBillingRequest message, RCMBilling request, IApiResponse response)
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

            _observabilityService.AddEvent(Observability.RcmBilling.BillRequestFailedEvent, new Dictionary<string, object>
            {
                {Observability.EventParams.EvaluationId, message.EvaluationId},
                {Observability.EventParams.CreatedDateTime, ((DateTimeOffset)message.BillableDate).ToUnixTimeSeconds()}
            });

            // Raise for NSB retry
            throw new RcmBillingRequestException(message.EvaluationId, response.StatusCode, exMessage, response.Error);
        }

        private static Guid GetBillId(long evaluationId, IApiResponse<Guid?> response)
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
            throw new RcmBillingRequestException(evaluationId, response.StatusCode,
                "BillId was not included in the API response");
        }
    }
}
