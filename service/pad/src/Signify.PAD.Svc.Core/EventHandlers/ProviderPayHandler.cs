using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Refit;
using Signify.PAD.Svc.Core.ApiClient;
using Signify.PAD.Svc.Core.ApiClient.Requests;
using Signify.PAD.Svc.Core.ApiClient.Response;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Constants;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Exceptions;
using Signify.PAD.Svc.Core.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Signify.Dps.Observability.Library.Services;

namespace Signify.PAD.Svc.Core.EventHandlers;

public class ProviderPayHandler : IHandleMessages<ProviderPayRequest>
{
    private readonly ILogger<ProviderPayHandler> _logger;
    private readonly IMapper _mapper;
    private readonly IProviderPayApi _providerPayApi;
    private readonly IMediator _mediator;
    private readonly IObservabilityService _observabilityService;

    public ProviderPayHandler(ILogger<ProviderPayHandler> logger, IMapper mapper,
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
        using var scope = _logger.BeginScope("Handle={EventName}, PadId={PadId}, EvaluationId={EvaluationId}", nameof(ProviderPayRequest), message.PadId,
            message.EvaluationId);

        _logger.LogDebug("Start Handle");

        _logger.LogInformation("Received request for provider pay");

        var providerPay = await ReadProviderPayFromDatabase(message.PadId, context.CancellationToken);
        if (providerPay is not null)
        {
            _logger.LogInformation("No further action will be taken as there is already an entry in {Table} table", nameof(ProviderPay));
            return;
        }

        var providerPayRequest = _mapper.Map<ProviderPayApiRequest>(message);
        var providerPayResponse = await _providerPayApi.SendProviderPayRequest(providerPayRequest);

        _observabilityService.AddEvent(Observability.ProviderPay.ProviderPayApiStatusCodeEvent, new Dictionary<string, object>()
        {
            { "EvaluationId", message.EvaluationId },
            { "StatusCode", providerPayResponse.StatusCode }
        });

        var paymentId = GetPaymentId(providerPayResponse, message);
        IsThisEntryMishandledInPadDb(providerPayResponse.StatusCode, paymentId);

        await RaiseSaveProviderPayEvent(context, message, paymentId);

        _observabilityService.AddEvent(Observability.ProviderPay.ProviderPayOrBillingEvent, new Dictionary<string, object>()
        {
            { "EvaluationId", message.EvaluationId },
            { "Type", "ProviderPay" }
        });

        _logger.LogDebug("End Handle");
    }

    /// <summary>
    /// Raise NServiceBus event: SaveProviderPay
    /// </summary>
    /// <param name="messageHandlerContext"></param>
    /// <param name="message"></param>
    /// <param name="paymentId"></param>
    [Trace]
    private static async Task RaiseSaveProviderPayEvent(IMessageHandlerContext messageHandlerContext, ProviderPayRequest message, string paymentId)
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

    /// <summary>
    /// Check if ProviderPay API returned 303 indicating a duplicate entry
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="paymentId"></param>
    /// <returns></returns>
    private void IsThisEntryMishandledInPadDb(HttpStatusCode statusCode, string paymentId)
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
        switch (apiResponse.StatusCode)
        {
            case HttpStatusCode.Accepted:
            {
                var paymentId = apiResponse.Content.PaymentId;
                _logger.LogInformation(
                    "ProviderPay API request completed with status code: {StatusCode} and received PaymentId: {PaymentId}",
                    apiResponse.StatusCode, paymentId);
                ValidatePaymentId(paymentId);
                return paymentId;
            }
            case HttpStatusCode.SeeOther:
            {
                var paymentId = ExtractPaymentIdFromHeader(apiResponse.Headers.Location?.ToString());
                _logger.LogInformation(
                    "ProviderPay API request completed with status code: {StatusCode} and received PaymentId: {PaymentId}",
                    apiResponse.StatusCode, paymentId);
                return paymentId;
            }
        }

        _logger.LogError("Error while trying to access ProviderPay API. Status Code: {StatusCode}",
            apiResponse.StatusCode);
        // Raise for NSB retry
        throw new ProviderPayRequestException(providerPayRequest.PadId, providerPayRequest.EvaluationId,
            apiResponse.StatusCode,
            "Unsuccessful HTTP status code returned from ProviderPay API", apiResponse.Error);

        void ValidatePaymentId(string pId)
        {
            if (!string.IsNullOrWhiteSpace(pId))
            {
                return;
            }

            _logger.LogError("Empty paymentId received from ProviderPay API");
            // Raise for NSB retry
            throw new ProviderPayRequestException(providerPayRequest.PadId, providerPayRequest.EvaluationId,
                apiResponse.StatusCode, "Empty paymentId received from ProviderPay API");
        }
    }

    /// <summary>
    /// Extract the PaymentId from the Location Header in case of 303 status code
    /// </summary>
    /// <param name="headersLocation">Location header value</param>
    /// <returns></returns>
    private static string ExtractPaymentIdFromHeader(string headersLocation) =>
        headersLocation?.Split('/').Last();

    /// <summary>
    /// Read ProviderPay entry from database based on the PadId
    /// </summary>
    /// <returns></returns>
    [Trace]
    private Task<ProviderPay> ReadProviderPayFromDatabase(int padId, CancellationToken cancellationToken)
    {
        var getProviderPay = new GetProviderPayByPadId
        {
            PadId = padId
        };
        return _mediator.Send(getProviderPay, cancellationToken);
    }
}