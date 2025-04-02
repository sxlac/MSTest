using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Refit;
using Signify.FOBT.Svc.Core.ApiClient.Requests;
using Signify.FOBT.Svc.Core.ApiClient.Response;
using Signify.FOBT.Svc.Core.ApiClient;
using Signify.FOBT.Svc.Core.BusinessRules;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Constants;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Exceptions;
using Signify.FOBT.Svc.Core.Infrastructure.Observability;
using Signify.FOBT.Svc.Core.Models;
using Signify.FOBT.Svc.Core.Queries;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public class ProviderPayHandler : IHandleMessages<ProviderPayRequest>
{
    private readonly ILogger<ProviderPayHandler> _logger;
    private readonly IMapper _mapper;
    private readonly IProviderPayApi _providerPayApi;
    private readonly IMediator _mediator;
    private readonly ITransactionSupplier _transactionSupplier;
    private readonly IPublishObservability _publishObservability;
    private readonly IPayableRules _payableRules;

    public ProviderPayHandler(ILogger<ProviderPayHandler> logger, IMapper mapper,
        IProviderPayApi providerPayApi, IMediator mediator,
        ITransactionSupplier transactionSupplier, IPublishObservability publishObservability, IPayableRules payableRules)
    {
        _logger = logger;
        _mapper = mapper;
        _providerPayApi = providerPayApi;
        _mediator = mediator;
        _transactionSupplier = transactionSupplier;
        _publishObservability = publishObservability;
        _payableRules = payableRules;
    }

    [Transaction]
    public async Task Handle(ProviderPayRequest message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Received provider pay request for EvaluationId={EvaluationId}, FobtId={ExamId}, EventId={EventId} ",
            message.EvaluationId, message.ExamId, message.EventId);
        using var transaction = _transactionSupplier.BeginTransaction();
        var exam = await ReadFobtExamFromDatabase(message.EvaluationId);

        // this is the null check pattern. It checks if the IsMet property of IsProviderPayable(exam)'s output is false and then assigns the output to rulesCheckResult
        if (await IsProviderPayable(exam) is { IsMet: false } rulesCheckResult)
        {
            await PublishStatus(message, FOBTStatusCode.ProviderNonPayableEventReceived, exam, rulesCheckResult.Reason);

            _logger.LogInformation(
                "Exam with EvaluationId={EvaluationId}, EventId={EventId} does not satisfy business rules to pay provider, with Reason={Reason}",
                message.EvaluationId, message.EventId, rulesCheckResult.Reason);

            await transaction.CommitAsync(context.CancellationToken);
            return;
        }

        await PublishStatus(message, FOBTStatusCode.ProviderPayableEventReceived, exam);
        var providerPay = await ReadProviderPayFromDatabase(message.ExamId);
        if (providerPay is not null)
        {
            _logger.LogInformation("No further action will be taken as there is already an entry in {Table} table", nameof(ProviderPay));
            await transaction.CommitAsync(context.CancellationToken);
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

        await transaction.CommitAsync(context.CancellationToken);
        _publishObservability.Commit();

        _logger.LogDebug("End Handle");
    }

    /// <summary>
    /// Publishes observability events
    /// </summary>
    /// <param name="message"></param>
    /// <param name="eventType"></param>
    /// <param name="eventParam"></param>
    /// <param name="eventParamValue"></param>
    /// <param name="sendImmediate"></param>
    /// <returns></returns>
    private void RegisterObservabilityEvent(ProviderPayRequest message, string eventType, string eventParam, object eventParamValue, bool sendImmediate = false)
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception while trying to add observability for EvaluationId={EvaluationId} EventId={EventId}",
                message.EvaluationId, message.EventId);
        }
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
            EventId = message.EventId,
            EvaluationId = message.EvaluationId,
            PaymentId = paymentId,
            ParentEventDateTime = message.ParentEventDateTime,
            ParentEventReceivedDateTime = message.ParentEventReceivedDateTime
        };
        await messageHandlerContext.SendLocal(saveProviderPay);
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
                _logger.LogInformation("ProviderPay API request completed with status code: {StatusCode} and received PaymentId: {PaymentId}",
                    apiResponse.StatusCode, paymentId);
                break;
            case HttpStatusCode.SeeOther:
                paymentId = ExtractPaymentIdFromHeader(apiResponse.Headers.Location?.ToString());
                _logger.LogWarning(
                    "Provider pay was missing from db for PaymentId={PaymentId} even though ProviderPay API says this entry has already been handled",
                    paymentId);
                break;
            default:
                // Raise for NSB retry
                _logger.LogError("Error while trying to access ProviderPay API. Status Code: {StatusCode}", apiResponse.StatusCode);
                throw new ProviderPayRequestException(providerPayRequest.ExamId, providerPayRequest.EvaluationId,
                    apiResponse.StatusCode, "Unsuccessful HTTP status code returned from ProviderPay API", apiResponse.Error);
        }

        if (string.IsNullOrWhiteSpace(paymentId))
        {
            // Raise for NSB retry
            throw new ProviderPayRequestException(providerPayRequest.ExamId, providerPayRequest.EvaluationId,
                apiResponse.StatusCode, "Empty paymentId received from ProviderPay API");
        }

        _logger.LogInformation("ProviderPay API request completed with StatusCode={StatusCode} and PaymentId={PaymentId}, for EvaluationId={EvaluationId}",
            apiResponse.StatusCode, paymentId, providerPayRequest.EvaluationId);

        return paymentId;
    }

    /// <summary>
    /// Invoke <see cref="ExamStatusEvent"/> as a Nsb event to write to database and optionally raise kafka event
    /// </summary>
    /// <param name="message"></param>
    /// <param name="statusCode"></param>
    /// <param name="exam"></param>
    /// <param name="reason"></param>
    private Task PublishStatus(ProviderPayRequest message, FOBTStatusCode statusCode, FOBT exam, string reason = null)
    {
        var statusEvent = new UpdateExamStatus
        {
            ExamStatus = new ProviderPayStatusEvent
            {
                Exam = exam,
                EvaluationId = message.EvaluationId,
                EventId = message.EventId,
                StatusCode = statusCode,
                StatusDateTime = message.ParentEventDateTime,
                ParentCdiEvent = message.ParentEvent,
                Reason = reason,
                ParentEventReceivedDateTime = message.ParentEventReceivedDateTime
            }
        };

        return _mediator.Send(statusEvent);
    }

    /// <summary>
    /// Extract the PaymentId from the Location Header in case of 303 status code
    /// </summary>
    /// <param name="headersLocation">Location header value</param>
    /// <returns></returns>
    private static string ExtractPaymentIdFromHeader(string headersLocation) => headersLocation?.Split('/').Last();

    /// <summary>
    /// Read ProviderPay entry from database based on the FobtId
    /// </summary>
    /// <param name="fobtId"></param>
    /// <returns></returns>
    [Trace]
    private async Task<ProviderPay> ReadProviderPayFromDatabase(int fobtId)
    {
        var getProviderPay = new GetProviderPayByFobtId
        {
            FOBTId = fobtId
        };
        var providerPay = await _mediator.Send(getProviderPay);

        return providerPay;
    }

    /// <summary>
    /// Read Exam entry from database based on the evaluationId
    /// </summary>
    /// <param name="evaluationId"></param>
    /// <returns></returns>
    [Trace]
    private async Task<FOBT> ReadFobtExamFromDatabase(long evaluationId)
    {
        var getExam = new GetFOBT
        {
            EvaluationId = evaluationId
        };
        var exam = await _mediator.Send(getExam);

        return exam;
    }


    /// <summary>
    /// Check the business rules.
    /// </summary>
    /// <param name="exam"></param>
    /// <returns></returns>
    private async Task<BusinessRuleStatus> IsProviderPayable(FOBT exam)
    {
        var validLabResultStatus = await _mediator.Send(new GetFobtStatusByStatusCodeAndEvaluationId
        {
            EvaluationId = exam.EvaluationId!.Value,
            FobtStatusCode = FOBTStatusCode.ValidLabResultsReceived
        });

        return _payableRules.IsPayable(new PayableRuleAnswers
        {
            IsValidLabResultsReceived = validLabResultStatus is not null
        });
    }
}