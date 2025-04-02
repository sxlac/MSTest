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
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.DEE.Svc.Core.ApiClient;
using Signify.DEE.Svc.Core.ApiClient.Requests;
using Signify.DEE.Svc.Core.ApiClient.Responses;
using Signify.DEE.Svc.Core.BusinessRules;
using Signify.DEE.Svc.Core.Commands;
using Signify.DEE.Svc.Core.Constants;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Events.Status;
using Signify.DEE.Svc.Core.Exceptions;
using Signify.DEE.Svc.Core.Messages.Models;
using Signify.DEE.Svc.Core.Messages.Queries;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;

namespace Signify.DEE.Svc.Core.EventHandlers.Nsb;

public class ProviderPayRequestHandler(
    ILogger<ProviderPayRequestHandler> logger,
    IMediator mediator,
    IMapper mapper,
    IProviderPayApi providerPayApi,
    IPublishObservability publishObservability,
    ITransactionSupplier transactionSupplier,
    IPayableRules payableRules)
    : IHandleMessages<ProviderPayRequest>
{
    public async Task Handle(ProviderPayRequest message, IMessageHandlerContext context)
    {
        logger.LogInformation("Received provider pay request for EvaluationId={EvaluationId}, ExamId={ExamId}, EventId={EventId}",
            message.EvaluationId, message.ExamId, message.EventId);
        using var transaction = transactionSupplier.BeginTransaction();

        // this is the null check pattern. It checks if the IsMet property of IsProviderPayable(exam)'s output is false and then assigns the output to rulesCheckResult
        if (await IsProviderPayable(message.ExamId) is { IsMet: false } rulesCheckResult)
        {
            await PublishStatus(message, ExamStatusCode.ProviderNonPayableEventReceived, rulesCheckResult.Reason);

            logger.LogInformation(
                "Exam with EvaluationId={EvaluationId}, EventId={EventId} does not satisfy business rules to pay provider, with Reason={Reason}",
                message.EvaluationId, message.EventId, rulesCheckResult.Reason);

            await CommitTransactions(transaction);
            return;
        }

        await PublishStatus(message, ExamStatusCode.ProviderPayableEventReceived);
        var providerPay = await GetProviderPay(message.ExamId);
        if (!string.IsNullOrEmpty(providerPay))
        {
            logger.LogInformation("Provider pay already processed for EvaluationId={EvaluationId} EventId={EventId}, nothing to do",
                message.EvaluationId, message.EventId);
            await CommitTransactions(transaction);
            return;
        }

        var censeoId = await GetCenseoId(message);
        var providerPayRequest = mapper.Map<ProviderPayApiRequest>(message);
        providerPayRequest.PersonId = censeoId;
        var providerPayResponse = await providerPayApi.SendProviderPayRequest(providerPayRequest);
        RegisterObservability(message, Observability.ProviderPay.ProviderPayApiStatusCodeEvent, Observability.EventParams.StatusCode,
            providerPayResponse?.StatusCode, true);
        RegisterObservability(message, Observability.ProviderPay.ProviderPayOrBillingEvent, Observability.EventParams.Type,
            Observability.EventParams.TypeProviderPay);
        var paymentId = GetPaymentId(providerPayResponse, message);
        await RaiseSaveProviderPayEvent(context, message, paymentId);
        await CommitTransactions(transaction);
        logger.LogInformation("Finished processing provider pay for EvaluationId={EvaluationId}", message.EvaluationId);
    }

    /// <summary>
    /// Get CenseoId from MemberApi
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    private async Task<string> GetCenseoId(ProviderPayRequest message)
    {
        var memberInfo = await mediator.Send(mapper.Map<GetMemberInfo>(message));
        if (string.IsNullOrWhiteSpace(memberInfo?.CenseoId))
        {
            throw new ProviderPayException(message.EvaluationId.ToString(), message.EventId, "Unable to retrieve CenseoId from MemberApi");
        }

        return memberInfo.CenseoId;
    }

    /// <summary>
    /// Check the business rules.
    /// </summary>
    /// <param name="examId"></param>
    /// <returns></returns>
    private async Task<BusinessRuleStatus> IsProviderPayable(int examId)
    {
        var answers = new PayableRuleAnswers
        {
            StatusCodes = await GetStatusCodesForExam(examId)
        };
        return payableRules.IsPayable(answers);
    }

    /// <summary>
    /// Invoke <see cref="ExamStatusEvent"/> as a Nsb event to write to database and optionally raise kafka event
    /// </summary>
    /// <param name="message"></param>
    /// <param name="statusCode"></param>
    /// <param name="reason"></param>
    private Task PublishStatus(ProviderPayRequest message, ExamStatusCode statusCode, string reason = null)
    {
        var status = mapper.Map<ProviderPayStatusEvent>(message);
        status.StatusCode = statusCode;
        status.Reason = reason;
        var updateEvent = new UpdateExamStatus
        {
            ExamStatus = status
        };

        return mediator.Send(updateEvent);
    }

    private void RegisterObservability(ProviderPayRequest message, string eventType, string eventParam, object eventParamValue, bool sendImmediate = false)
    {
        try
        {
            var observabilityEvent = new ObservabilityEvent
            {
                EvaluationId = message.EvaluationId,
                EventId = message.EventId,
                EventType = eventType,
                EventValue = new Dictionary<string, object>
                {
                    { Observability.EventParams.EvaluationId, message.EvaluationId },
                    { eventParam, eventParamValue }
                }
            };

            publishObservability.RegisterEvent(observabilityEvent, sendImmediate);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Exception while trying to add observability for EvaluationId={EvaluationId} EventId={EventId}",
                message.EvaluationId, message.EventId);
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
                logger.LogWarning(
                    "Provider pay was missing from db for PaymentId={PaymentId} even though ProviderPay API says this entry has already been handled",
                    paymentId);
                break;
            default:
                // Raise for NSB retry
                throw new ProviderPayException(providerPayRequest.EvaluationId.ToString(), providerPayRequest.EventId,
                    $"Unsuccessful HTTP status code: {apiResponse.StatusCode} returned from ProviderPay API", apiResponse.Error);
        }

        if (string.IsNullOrWhiteSpace(paymentId))
        {
            // Raise for NSB retry
            throw new ProviderPayException(providerPayRequest.EvaluationId.ToString(), providerPayRequest.EventId,
                $"Empty paymentId received from ProviderPay API with status code: {apiResponse.StatusCode}");
        }

        logger.LogInformation("ProviderPay API request completed with StatusCode={StatusCode} and PaymentId={PaymentId}, for EvaluationId={EvaluationId}",
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
    /// Raise NServiceBus event: <see cref="SaveProviderPay"/>
    /// </summary>
    /// <param name="messageHandlerContext"></param>
    /// <param name="message"></param>
    /// <param name="paymentId"></param>
    [Trace]
    private Task RaiseSaveProviderPayEvent(IMessageHandlerContext messageHandlerContext, ProviderPayRequest message, string paymentId)
    {
        var saveProviderPay = mapper.Map<SaveProviderPay>(message);
        saveProviderPay.PaymentId = paymentId;
        return messageHandlerContext.SendLocal(saveProviderPay);
    }

    /// <summary>
    /// Read ProviderPay entry from database based on the ExamId
    /// </summary>
    /// <param name="examId"></param>
    /// <returns></returns>
    [Trace]
    private Task<string> GetProviderPay(int examId)
    {
        var getProviderPay = new GetProviderPayId(examId);
        return mediator.Send(getProviderPay);
    }

    /// <summary>
    /// Commit both TransactionSupplier and Observability transactions
    /// </summary>
    /// <param name="transaction">The transaction generated as part of TransactionSupplier.BeginTransaction</param>
    [Trace]
    private async Task CommitTransactions(IBufferedTransaction transaction)
    {
        await transaction.CommitAsync();
        publishObservability.Commit();
    }

    /// <summary>
    /// Fetches the status codes associated with the examId
    /// </summary>
    /// <param name="examId"></param>
    /// <returns></returns>
    private Task<List<int>> GetStatusCodesForExam(int examId)
    {
        return mediator.Send(new GetAllExamStatus(examId));
    }
}