using AutoMapper;
using EgfrNsbEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Newtonsoft.Json;
using NServiceBus;
using Refit;
using Signify.eGFR.Core.ApiClients.RcmApi.Requests;
using Signify.eGFR.Core.ApiClients.RcmApi;
using Signify.eGFR.Core.Constants;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Exceptions;
using Signify.eGFR.Core.Infrastructure;
using Signify.eGFR.Core.Queries;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System;
using Signify.Dps.Observability.Library.Services;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public class CreateBillHandler(
    ILogger<CreateBillHandler> logger,
    IMediator mediator,
    ITransactionSupplier transactionSupplier,
    IPublishObservability publishObservability,
    IApplicationTime applicationTime,
    IMapper mapper,
    IRcmApi rcmApi)
    : BaseNsbHandler(logger, mediator, transactionSupplier, publishObservability, applicationTime), IHandleMessages<CreateBillEvent>
{
    [Transaction]
    public async Task Handle(CreateBillEvent message, IMessageHandlerContext context)
    {
        using var transaction = TransactionSupplier.BeginTransaction();
        
        var exam = await Mediator.Send(new QueryExam(message.EvaluationId), context.CancellationToken);

        var createBillRequest = mapper.Map<CreateBillRequest>(exam);
        mapper.Map(message, createBillRequest);
        createBillRequest.CorrelationId = message.EventId.ToString();
        createBillRequest.RcmProductCode = message.RcmProductCode;
        
        if (Logger.IsEnabled(LogLevel.Debug))
        {
            Logger.LogDebug("Sending billing request to RCM API for EvaluationId={EvaluationId} with CorrelationId={CorrelationId}: Request={Request}",
                message.EvaluationId, createBillRequest.CorrelationId, JsonConvert.SerializeObject(createBillRequest));
        }
        else
        {
            Logger.LogInformation("Sending billing request to RCM API for EvaluationId={EvaluationId} with CorrelationId={CorrelationId}",
                message.EvaluationId, createBillRequest.CorrelationId);
        }

        var response = await rcmApi.SendBillingRequest(createBillRequest);

        PublishObservabilityEvents(message.EvaluationId, ApplicationTime.UtcNow(),
            Observability.ProviderPay.RcmBillingApiStatusCodeEvent,
            new Dictionary<string, object> {
                {Observability.EventParams.EventId, message.EventId},
                {Observability.EventParams.StatusCode, response?.StatusCode} }, true);

        PublishObservabilityEvents(message.EvaluationId, ApplicationTime.UtcNow(),
            Observability.ProviderPay.ProviderPayOrBillingEvent,
            new Dictionary<string, object> {
                {Observability.EventParams.EventId, message.EventId},
                {Observability.EventParams.Type, Observability.EventParams.TypeRcmBilling} });

        ValidateSuccessful(message, response);

        var billId = GetBillId(message, response);

        PublishObservabilityEvents(message.EvaluationId, message.BillableDate,
            Observability.RcmBilling.BillRequestRaisedEvent,
            new Dictionary<string, object> {{Observability.EventParams.BillId, billId}}, true);

        await context.SendLocal(new BillRequestSentEvent
        {
            EventId = message.EventId,
            EvaluationId = message.EvaluationId,
            BillId = billId,
            ExamId = exam.ExamId,
            RcmProductCode = message.RcmProductCode
        });

        await context.SendLocal(new BillableExamStatusEvent
        {
            EventId = message.EventId,
            ExamId = exam.ExamId,
            StatusCode = ExamStatusCode.BillRequestSent,
            EvaluationId = exam.EvaluationId,
            StatusDateTime = ApplicationTime.UtcNow(),
            PdfDeliveryDateTime = message.PdfDeliveryDateTime,
            BillId = billId,
            RcmProductCode = message.RcmProductCode
        });
        
        await CommitTransactions(transaction, context.CancellationToken);
    }
    
     private void ValidateSuccessful(CreateBillEvent request, IApiResponse response)
    {
        // See: https://wiki.signifyhealth.com/display/BILL/Integration+Guide
        // Response codes returned by API:
        // Accepted (202)
        // Moved Permanently (301)
        // Bad Request (400)
        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.MovedPermanently)
        {
            Logger.LogInformation("Received StatusCode={StatusCode} from RCM API, for EvaluationId={EvaluationId}", response.StatusCode, request.EvaluationId);
            return;
        }

        if (response.StatusCode is >= HttpStatusCode.BadRequest and < HttpStatusCode.InternalServerError)
            Logger.LogWarning("Received StatusCode={StatusCode} from RCM API for EvaluationId={EvaluationId} with Request={Request}", response.StatusCode, request.EvaluationId, JsonConvert.SerializeObject(request));

        var message = "Unsuccessful HTTP status code returned";
        if (!string.IsNullOrEmpty(response.Error?.Content))
            message = $"{message}, with response: {response.Error.Content}"; // For 400, RCM includes the failure reason in the response content; see https://wiki.signifyhealth.com/display/BILL/Integration+Guide

        PublishObservabilityEvents(request.EvaluationId, request.BillableDate,
            Observability.RcmBilling.BillRequestFailedEvent, null, true);
        
        // Raise for NSB retry
        throw new RcmBillingRequestException(request.EventId, request.EvaluationId, response.StatusCode,
            message, response.Error);
    }

    private static Guid GetBillId(CreateBillEvent request, IApiResponse<Guid?> response)
    {
        // 200-level response codes will have the BillId guid in the content
        if (response.Content.HasValue)
        {
            return response.Content.Value;
        }

        // 300-level response codes will have the BillId guid in the error content
        if (response.Error?.Content == null)
            throw new RcmBillingRequestException(request.EventId, request.EvaluationId, response.StatusCode,
                "BillId was not included in the API response");
            
        var billId = JsonConvert.DeserializeObject<Guid>(response.Error.Content);
        if (billId != Guid.Empty)
            return billId;

        // Raise for NSB retry
        throw new RcmBillingRequestException(request.EventId, request.EvaluationId, response.StatusCode,
            "BillId was not included in the API response");
    }
}