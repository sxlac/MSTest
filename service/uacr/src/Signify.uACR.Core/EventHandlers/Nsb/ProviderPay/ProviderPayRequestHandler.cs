using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Refit;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.uACR.Core.ApiClients.ProviderPayAPi.Requests;
using Signify.uACR.Core.ApiClients.ProviderPayAPi.Responses;
using Signify.uACR.Core.ApiClients.ProviderPayAPi;
using Signify.uACR.Core.BusinessRules;
using Signify.uACR.Core.Commands;
using Signify.uACR.Core.Constants;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Data;
using Signify.uACR.Core.Events.Status;
using Signify.uACR.Core.Exceptions;
using Signify.uACR.Core.Models;
using Signify.uACR.Core.Queries;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using Signify.Dps.Observability.Library.Services;
using Signify.uACR.Core.Infrastructure;
using UacrNsbEvents;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public class ProviderPayRequestHandler(
    ILogger<ProviderPayRequestHandler> logger,
    IMapper mapper,
    IProviderPayApi providerPayApi,
    IMediator mediator,
    IPublishObservability publishObservability,
    ITransactionSupplier transactionSupplier,
    IPayableRules payableRules,
    IApplicationTime applicationTime)
    : BaseNsbHandler(logger, mediator, transactionSupplier, publishObservability, applicationTime), IHandleMessages<ProviderPayRequest>
{
    [Transaction]
    public async Task Handle(ProviderPayRequest message, IMessageHandlerContext context)
    {
        Logger.LogInformation("Received provider pay request for EvaluationId={EvaluationId}, ExamId={ExamId}, EventId={EventId} ",
            message.EvaluationId, message.ExamId, message.EventId);
        using var transaction = TransactionSupplier.BeginTransaction();

        //this is the null check pattern. It checks if the IsMet property of IsProviderPayable(exam)'s output is false and then assigns the output to rulesCheckResult
        if (await IsProviderPayable(message, context.CancellationToken) is { IsMet: false } rulesCheckResult)
        {
            await PublishStatus(message, ExamStatusCode.ProviderNonPayableEventReceived, context.CancellationToken, rulesCheckResult.Reason);
        
            Logger.LogInformation(
                "Exam with EvaluationId={EvaluationId}, EventId={EventId} does not satisfy business rules to pay provider, with Reason={Reason}",
                message.EvaluationId, message.EventId, rulesCheckResult.Reason);
        
            await CommitTransactions(transaction, context.CancellationToken);
            return;
        }

        await PublishStatus(message, ExamStatusCode.ProviderPayableEventReceived, context.CancellationToken);

        var providerPay = await GetProviderPay(message.ExamId, context.CancellationToken);
        if (providerPay is not null)
        {
            Logger.LogInformation("Provider pay already processed for EvaluationId={EvaluationId}, nothing to do", message.EvaluationId);
            await CommitTransactions(transaction, context.CancellationToken);
            return;
        }

        var providerPayRequest = mapper.Map<ProviderPayApiRequest>(message);
        var providerPayResponse = await providerPayApi.SendProviderPayRequest(providerPayRequest);

        PublishObservabilityEvents(message.EvaluationId, ApplicationTime.UtcNow(),
            Observability.ProviderPay.ProviderPayApiStatusCodeEvent,
            new Dictionary<string, object>()
            {
                { Observability.EventParams.EventId, message.EventId },
                { Observability.EventParams.StatusCode, providerPayResponse?.StatusCode }
            }, true);

        PublishObservabilityEvents(message.EvaluationId, ApplicationTime.UtcNow(),
            Observability.ProviderPay.ProviderPayOrBillingEvent,
            new Dictionary<string, object>()
            {
                { Observability.EventParams.EventId, message.EventId },
                { Observability.EventParams.Type, Observability.EventParams.TypeProviderPay }
            }, true);

        var paymentId = GetPaymentId(providerPayResponse, message);

        await RaiseSaveProviderPayEvent(context, message, paymentId);

        await CommitTransactions(transaction, context.CancellationToken);
        Logger.LogInformation("Finished processing provider pay for EvaluationId={EvaluationId}", message.EvaluationId);
    }

    /// <summary>
    /// Check the business rules
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    private async Task<BusinessRuleStatus> IsProviderPayable(ProviderPayRequest message, CancellationToken token)
    {
        var answers = new PayableRuleAnswers(message.EvaluationId, message.EventId)
        {
            Result = await Mediator.Send(new QueryLabResultByEvaluationId(message.EvaluationId), token)
        };
        return payableRules.IsPayable(answers);
    }

    /// <summary>
    /// Invoke <see cref="ExamStatusEvent"/> as a Nsb event to write to database and optionally raise kafka event
    /// </summary>
    /// <param name="message"></param>
    /// <param name="statusCode"></param>
    /// <param name="reason"></param>
    private Task PublishStatus(ProviderPayRequest message, ExamStatusCode statusCode, CancellationToken token, string reason = null)
    {
        var status = mapper.Map<ProviderPayStatusEvent>(message);
        status.StatusCode = statusCode;
        status.Reason = reason;
        var updateEvent = new UpdateExamStatus
        {
            ExamStatus = status
        };

        return Mediator.Send(updateEvent, token);
    }

    [Trace]
    private Task<ProviderPay> GetProviderPay(int examId, CancellationToken token)
    {
        var getProviderPay = new QueryProviderPayByExamId
        {
            ExamId = examId
        };
        return Mediator.Send(getProviderPay, token);
    }

    /// Extract the payment Id from response body or location header
    /// depending on status code
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
                Logger.LogWarning(
                    "Provider pay was missing from db for PaymentId={PaymentId} even though ProviderPay API says this entry has already been handled",
                    paymentId);
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

        Logger.LogInformation("ProviderPay API request completed with StatusCode={StatusCode} and PaymentId={PaymentId}, for EvaluationId={EvaluationId}",
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
    private Task RaiseSaveProviderPayEvent(IMessageHandlerContext messageHandlerContext,
        ProviderPayRequest message, string paymentId)
    {
        var saveProviderPay = mapper.Map<SaveProviderPay>(message);
        saveProviderPay.ParentEventDateTime = message.ParentEventDateTime;
        saveProviderPay.PaymentId = paymentId;
        return messageHandlerContext.SendLocal(saveProviderPay);
    }
}