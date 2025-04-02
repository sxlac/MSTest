using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Refit;
using Signify.HBA1CPOC.Svc.Core.ApiClient;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Requests;
using Signify.HBA1CPOC.Svc.Core.ApiClient.Response;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Constants;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.Svc.Core.Exceptions;
using Signify.HBA1CPOC.Svc.Core.Infrastructure.Observability;
using Signify.HBA1CPOC.Svc.Core.Queries;

namespace Signify.HBA1CPOC.Svc.Core.EventHandlers;

public class ProviderPayRequestHandler : IHandleMessages<ProviderPayRequest>
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IProviderPayApi _providerPayApi;
    private readonly IMediator _mediator;
    private readonly IPublishObservability _publishObservability;
    
    public ProviderPayRequestHandler(ILogger<ProviderPayRequestHandler> logger, IMapper mapper,
        IProviderPayApi providerPayApi, IMediator mediator, IPublishObservability publishObservability)
    {
        _logger = logger;
        _mapper = mapper;
        _providerPayApi = providerPayApi;
        _mediator = mediator;
        _publishObservability = publishObservability;
    }

    [Transaction]
    public async Task Handle(ProviderPayRequest message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Received provider pay request for EvaluationId={EvaluationId}", message.EvaluationId);

        var providerPay = await GetProviderPay(message.HBA1CPOCId);
        if (providerPay is not null)
        {
            _logger.LogInformation("Provider pay already processed for EvaluationId={EvaluationId}, nothing to do", message.EvaluationId);
            return;
        }

        var providerPayRequest = _mapper.Map<ProviderPayApiRequest>(message);
        var providerPayResponse = await _providerPayApi.SendProviderPayRequest(providerPayRequest);

        RegisterObservabilityEvent(message, Observability.ProviderPay.ProviderPayApiStatusCodeEvent, Observability.EventParams.StatusCode,
            providerPayResponse?.StatusCode, true);
        
        var paymentId = GetPaymentId(providerPayResponse, message);

        await RaiseSaveProviderPayEvent(context, message, paymentId);

        RegisterObservabilityEvent(message, Observability.ProviderPay.ProviderPayOrBillingEvent, Observability.EventParams.Type,
            Observability.EventParams.TypeProviderPay);
        
        _publishObservability.Commit();
        _logger.LogInformation("Finished processing provider pay for EvaluationId={EvaluationId}", message.EvaluationId);
    }

    [Trace]
    private Task<ProviderPay> GetProviderPay(int hba1cpocId)
    {
        var getProviderPay = new GetProviderPayByHba1CpocId
        {
            HBA1CPOCId = hba1cpocId
        };
        return _mediator.Send(getProviderPay);
    }

    /// <summary>
    /// Extract the payment Id from response body or location header
    /// depending on status code
    /// </summary>
    /// <param name="apiResponse">Response from API call</param>
    /// <param name="providerPayRequest"></param>
    /// <returns></returns>
    [Trace]
    private string GetPaymentId(IApiResponse<ProviderPayApiResponse> apiResponse, ProviderPayRequest providerPayRequest)
    {
        string paymentId;
        switch (apiResponse.StatusCode)
        {
            case HttpStatusCode.Accepted:
                paymentId = apiResponse.Content.PaymentId;
                break;
            case HttpStatusCode.SeeOther:
                paymentId = ExtractPaymentIdFromHeader(apiResponse.Headers.Location?.ToString());
                _logger.LogWarning("Provider pay was missing from db for PaymentId={PaymentId} even though ProviderPay API says this entry has already been handled", paymentId);
                break;
            default:
                // Raise for NSB retry
                throw new ProviderPayRequestException(providerPayRequest.EventId, providerPayRequest.EvaluationId,
                    apiResponse.StatusCode, "Unsuccessful HTTP status code returned from ProviderPay API", apiResponse.Error);
        }

        if (string.IsNullOrWhiteSpace(paymentId))
        {
            // Raise for NSB retry
            throw new ProviderPayRequestException(providerPayRequest.EventId, providerPayRequest.EvaluationId,
                apiResponse.StatusCode, "Empty paymentId received from ProviderPay API");
        }

        _logger.LogInformation("ProviderPay API request completed with StatusCode={StatusCode} and PaymentId={PaymentId}, for EvaluationId={EvaluationId}",
            apiResponse.StatusCode, paymentId, providerPayRequest.EvaluationId);

        return paymentId;
    }

    /// <summary>
    /// Extract the PaymentId from the Location Header in case of 303 status code
    /// </summary>
    /// <param name="headersLocation">Location header value</param>
    /// <returns></returns>
    private static string ExtractPaymentIdFromHeader(string headersLocation) => headersLocation?.Split('/').Last();

    /// <summary>
    /// Raise NServiceBus event: <see cref="SaveProviderPay"/>
    /// </summary>
    /// <param name="messageHandlerContext"></param>
    /// <param name="message"></param>
    /// <param name="paymentId"></param>
    [Trace]
    private static async Task RaiseSaveProviderPayEvent(IMessageHandlerContext messageHandlerContext,
        ProviderPayRequest message, string paymentId)
    {
        var saveProviderPay = new SaveProviderPay
        {
            EventId = Guid.NewGuid(),
            EvaluationId = message.EvaluationId,
            PaymentId = paymentId,
            PdfDeliveryDateTime = message.PdfDeliveryDateTime
        };
        await messageHandlerContext.SendLocal(saveProviderPay);
    }
    
    private void RegisterObservabilityEvent(ProviderPayRequest message, string eventType, string eventParam, object eventParamValue, bool sendImmediate = false)
    {
        var observabilityEvent = new ObservabilityEvent
        {
            EvaluationId = message.EvaluationId,
            EventId = message.EventId.ToString(),
            EventType = eventType,
            EventValue = new Dictionary<string, object>
            {
                { Observability.EventParams.EvaluationId, message.EvaluationId },
                { eventParam, eventParamValue }
            }
        };

        _publishObservability.RegisterEvent(observabilityEvent, sendImmediate);
    }
}
