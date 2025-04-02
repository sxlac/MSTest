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
using Signify.CKD.Svc.Core.ApiClient;
using Signify.CKD.Svc.Core.ApiClient.Requests;
using Signify.CKD.Svc.Core.ApiClient.Response;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.Constants;
using Signify.CKD.Svc.Core.Data.Entities;
using Signify.CKD.Svc.Core.Exceptions;
using Signify.CKD.Svc.Core.Infrastructure.Observability;
using Signify.CKD.Svc.Core.Queries;

namespace Signify.CKD.Svc.Core.EventHandlers;

public class ProviderPayRequestHandler : IHandleMessages<ProviderPayRequest>
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IProviderPayApi _providerPayApi;
    private readonly IMediator _mediator;
    private readonly IObservabilityService _observabilityService;

    public ProviderPayRequestHandler(ILogger<ProviderPayRequestHandler> logger, IMapper mapper,
        IProviderPayApi providerPayApi, IMediator mediator, IObservabilityService observabilityService)
    {
        _logger = logger;
        _mapper = mapper;
        _providerPayApi = providerPayApi;
        _mediator = mediator;
        _observabilityService = observabilityService;
    }
    
    [Transaction]
    public async Task Handle(ProviderPayRequest message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Received provider pay request for EvaluationId={EvaluationId} with CkdId={CkdId}", message.EvaluationId, message.CkdId);

        var providerPay = await GetProviderPay(message.CkdId);
        if (providerPay is not null)
        {
            _logger.LogInformation("Provider pay already processed for EvaluationId={EvaluationId}, nothing to do", message.EvaluationId);
            return;
        }

        var providerPayRequest = _mapper.Map<ProviderPayApiRequest>(message);
        var providerPayResponse = await _providerPayApi.SendProviderPayRequest(providerPayRequest);

        _observabilityService.AddEvent(Observability.ProviderPay.ProviderPayApiStatusCodeEvent, new Dictionary<string, object>
        {
            {Observability.EventParams.EvaluationId, message.EvaluationId},
            {Observability.EventParams.StatusCode,providerPayResponse?.StatusCode}
        });
        
        var paymentId = GetPaymentId(providerPayResponse, message);
        CheckIfThisEntryWasMishandledInCkdDb(providerPayResponse.StatusCode, paymentId);

        await RaiseSaveProviderPayEvent(context, message, paymentId);
        
        _observabilityService.AddEvent(Observability.ProviderPay.ProviderPayOrBillingEvent, new Dictionary<string, object>()
        {
            {Observability.EventParams.EvaluationId, message.EvaluationId},
            {Observability.EventParams.Type, "ProviderPay"}
        });

        _logger.LogInformation("Finished processing provider pay for EvaluationId={EvaluationId}", message.EvaluationId);
    }

    /// <summary>
    /// Raise NServiceBus event: <see cref="SaveProviderPay"/>
    /// </summary>
    /// <param name="messageHandlerContext"></param>
    /// <param name="message"></param>
    /// <param name="paymentId"></param>
    [Trace]
    private static Task RaiseSaveProviderPayEvent(IMessageHandlerContext messageHandlerContext, ProviderPayRequest message, string paymentId)
    {
        var saveProviderPay = new SaveProviderPay
        {
            EventId = Guid.NewGuid(),
            EvaluationId = message.EvaluationId,
            PaymentId = paymentId,
            PdfDeliveryDateTime = message.PdfDeliveryDateTime
        };
        return messageHandlerContext.SendLocal(saveProviderPay);
    }

    /// <summary>
    /// Check if ProviderPay API returned 303 indicating a duplicate entry
    /// when the entry was not present in CKD database ProviderPay table
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="paymentId"></param>
    /// <returns></returns>
    private void CheckIfThisEntryWasMishandledInCkdDb(HttpStatusCode statusCode, string paymentId)
    {
        if (statusCode == HttpStatusCode.SeeOther)
        {
            _logger.LogWarning(
                "Item missing from {Table} table for PaymentId: {PaymentId} even though ProviderPay API says this entry has already been handled",
                nameof(ProviderPay), paymentId);
        }
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
                throw new ProviderPayRequestException(providerPayRequest.CkdId, providerPayRequest.EvaluationId,
                    apiResponse.StatusCode, "Unsuccessful HTTP status code returned from ProviderPay API", apiResponse.Error);
        }

        if (string.IsNullOrWhiteSpace(paymentId))
        {
            // Raise for NSB retry
            throw new ProviderPayRequestException(providerPayRequest.CkdId, providerPayRequest.EvaluationId,
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
    private static string ExtractPaymentIdFromHeader(string headersLocation) =>
        headersLocation?.Split('/').Last();

    /// <summary>
    /// Read ProviderPay entry from database based on the CkdId
    /// </summary>
    /// <param name="ckdId"></param>
    /// <returns></returns>
    [Trace]
    private Task<ProviderPay> GetProviderPay(int ckdId)
    {
        var getProviderPay = new GetProviderPayByCkdId
        {
            CkdId = ckdId
        };
        return _mediator.Send(getProviderPay);
    }
}