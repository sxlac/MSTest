using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Refit;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.Spirometry.Core.ApiClients.ProviderPayApi;
using Signify.Spirometry.Core.ApiClients.ProviderPayApi.Requests;
using Signify.Spirometry.Core.ApiClients.ProviderPayApi.Responses;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Constants;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Events;
using Signify.Spirometry.Core.Exceptions;
using Signify.Spirometry.Core.Queries;
using StatusCode = Signify.Spirometry.Core.Models.StatusCode;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace SpiroNsb.SagaCommands;

public class ProcessProviderPay : ISagaCommand
{
    public long EvaluationId { get; set; }
    public bool IsPayable { get; set; }

    public ProcessProviderPay(long evaluationId, bool isPayable)
    {
        EvaluationId = evaluationId;
        IsPayable = isPayable;
    }
}

public class ProcessProviderPayHandler : IHandleMessages<ProcessProviderPay>
{
    private readonly ILogger<ProcessProviderPayHandler> _logger;
    private readonly ITransactionSupplier _transactionSupplier;
    private readonly IPublishObservability _publishObservability;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly IProviderPayApi _providerPayApi;

    public ProcessProviderPayHandler(ILogger<ProcessProviderPayHandler> logger, ITransactionSupplier transactionSupplier,
        IPublishObservability publishObservability,
        IMediator mediator, IMapper mapper, IProviderPayApi providerPayApi)
    {
        _logger = logger;
        _transactionSupplier = transactionSupplier;
        _publishObservability = publishObservability;
        _mediator = mediator;
        _mapper = mapper;
        _providerPayApi = providerPayApi;
    }

    [Transaction]
    public async Task Handle(ProcessProviderPay message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Start of ProcessPayment for EvaluationId={EvaluationId}", message.EvaluationId);

        using var transaction = _transactionSupplier.BeginTransaction();
        var eventId = Guid.NewGuid();
        var exam = await GetExam(message.EvaluationId, eventId, context.CancellationToken);
        var newCdiEvents = (await _mediator.Send(new QueryUnprocessedCdiEventForPayments(message.EvaluationId), context.CancellationToken)).ToList();
        if (!newCdiEvents.Any())
        {
            _logger.LogInformation("There are no cdi events to process payment for EvaluationId={EvaluationId}, EventId={EventId}",
                exam.EvaluationId, eventId);
            await CommitTransactions(transaction, context.CancellationToken);
            return;
        }

        await AddNewCdiEventsToDatabase(newCdiEvents, exam, context.CancellationToken);

        if (!IsEventTypePayable(newCdiEvents))
        {
            var failedWithoutPayEvent = newCdiEvents.OrderBy(e => e.CreatedDateTime)
                .Last(e => e.EventType == nameof(CDIFailedEvent) && !e.PayProvider!.Value!);
            await HandleNonPayableScenario(failedWithoutPayEvent, exam, "PayProvider is false for the CDIFailedEvent", context.CancellationToken);
            await CommitTransactions(transaction, context.CancellationToken);
            return;
        }

        var latestCdiEvent = newCdiEvents.OrderBy(e => e.DateTime).Last();
        if (!await IsPayable(exam.EvaluationId, latestCdiEvent.RequestId, context.CancellationToken))
        {
            await HandleNonPayableScenario(latestCdiEvent, exam, "Payment rules are not satisfied", context.CancellationToken);
            await CommitTransactions(transaction, context.CancellationToken);
            return;
        }

        await PublishStatus(latestCdiEvent, StatusCode.ProviderPayableEventReceived, exam, token: context.CancellationToken);

        var providerPayEntry = await _mediator.Send(new QueryProviderPay(message.EvaluationId), context.CancellationToken);
        if (providerPayEntry != null)
        {
            _logger.LogInformation("Provider pay already processed for EvaluationId={EvaluationId} EventId={EventId}, nothing to do",
                message.EvaluationId, eventId);
            await CommitTransactions(transaction, context.CancellationToken);
            return;
        }

        var paymentId = await InvokeProviderPayApi(exam, latestCdiEvent.RequestId);
        await RaiseSaveProviderPayEvent(latestCdiEvent, paymentId, exam.SpirometryExamId, context);

        await CommitTransactions(transaction, context.CancellationToken);

        _logger.LogInformation("End of ProcessPayment for EvaluationId={EvaluationId}", message.EvaluationId);
    }

    /// <summary>
    /// Raise NServiceBus event: SaveProviderPay
    /// </summary>
    /// <param name="cdiEvent"></param>
    /// <param name="paymentId"></param>
    /// <param name="examId"></param>
    /// <param name="context"></param>
    [Trace]
    private async Task RaiseSaveProviderPayEvent(CdiEventForPayment cdiEvent, string paymentId, int examId, IMessageHandlerContext context)
    {
        var saveProviderPay = _mapper.Map<SaveProviderPay>(cdiEvent);
        saveProviderPay.ExamId = examId;
        saveProviderPay.PaymentId = paymentId;
        saveProviderPay.Context = context;
        await _mediator.Send(saveProviderPay, context.CancellationToken);
    }

    /// <summary>
    /// Send Kafka event - ProviderNonPayableEventReceived
    /// </summary>
    private async Task HandleNonPayableScenario(CdiEventForPayment cdiEvent, SpirometryExam exam, string reason, CancellationToken token)
    {
        await PublishStatus(cdiEvent, StatusCode.ProviderNonPayableEventReceived, exam, reason, token);
        _logger.LogInformation(
            "Exam with EvaluationId={EvaluationId}, EventId={EventId} is not eligible for payment as {Reason}",
            exam.EvaluationId, cdiEvent.RequestId, reason);
    }

    /// <summary>
    /// Invoke ProviderPay API to get the paymentId
    /// </summary>
    /// <param name="exam"></param>
    /// <param name="eventId"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task<string> InvokeProviderPayApi(SpirometryExam exam, Guid eventId)
    {
        var providerPayApiRequest = _mapper.Map<ProviderPayApiRequest>(exam);
        providerPayApiRequest.AdditionalDetails = new Dictionary<string, string>
        {
            { "EvaluationId", exam.EvaluationId.ToString() },
            { "AppointmentId", exam.AppointmentId.ToString() }
        };
        var providerPayResponse = await _providerPayApi.SendProviderPayRequest(providerPayApiRequest);
        RegisterObservabilityEvent(exam, eventId, Observability.ProviderPay.ProviderPayApiStatusCodeEvent, Observability.EventParams.StatusCode,
            providerPayResponse?.StatusCode, true);
        RegisterObservabilityEvent(exam, eventId, Observability.ProviderPay.ProviderPayOrBillingEvent, Observability.EventParams.Type,
            Observability.EventParams.TypeProviderPay);
        return GetPaymentId(providerPayResponse, exam);
    }

    /// <summary>
    /// Check if the payment rules are satisfied for the exam and returns the latest cdi event
    /// </summary>
    /// <param name="evaluationId"></param>
    /// <param name="eventId"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task<bool> IsPayable(long evaluationId, Guid eventId, CancellationToken token)
    {
        return (await _mediator.Send(new QueryPayable(eventId, evaluationId), token)).IsPayable;
    }

    /// <summary>
    /// Extract the payment Id from response body or location header
    /// depending on status code
    /// </summary>
    /// <param name="apiResponse">Response from API call</param>
    /// <param name="exam"></param>
    /// <returns></returns>
    [Trace]
    private string GetPaymentId(IApiResponse<ProviderPayApiResponse> apiResponse, SpirometryExam exam)
    {
        string paymentId;
        switch (apiResponse.StatusCode)
        {
            case HttpStatusCode.Accepted:
                paymentId = apiResponse.Content.PaymentId;
                _logger.LogInformation(
                    "ProviderPay API request completed with status code={StatusCode} and received PaymentId={PaymentId} for EvaluationId={EvaluationId}",
                    apiResponse.StatusCode, paymentId, exam.EvaluationId);
                break;
            case HttpStatusCode.SeeOther:
                paymentId = ExtractPaymentIdFromHeader(apiResponse.Headers.Location?.ToString());
                _logger.LogInformation(
                    "Provider pay was missing from db for PaymentId={PaymentId} even though ProviderPay API says this entry has already been handled; for EvaluationId={EvaluationId}",
                    paymentId, exam.EvaluationId);
                break;
            default:
                // Raise for NSB retry
                _logger.LogError("Error while trying to access ProviderPay API. Status Code: {StatusCode} for EvaluationId={EvaluationId}",
                    apiResponse.StatusCode, exam.EvaluationId);
                throw new ProviderPayRequestException(exam.SpirometryExamId, exam.EvaluationId, apiResponse.StatusCode,
                    "Unsuccessful HTTP status code returned from ProviderPay API", apiResponse.Error);
        }

        if (string.IsNullOrWhiteSpace(paymentId))
        {
            // Raise for NSB retry
            throw new ProviderPayRequestException(exam.SpirometryExamId, exam.EvaluationId,
                apiResponse.StatusCode, $"Empty paymentId received from ProviderPay API for EvaluationId={exam.EvaluationId}");
        }

        _logger.LogInformation("ProviderPay API request completed with StatusCode={StatusCode} and PaymentId={PaymentId}, for EvaluationId={EvaluationId}",
            apiResponse.StatusCode, paymentId, exam.EvaluationId);

        return paymentId;
    }

    /// <summary>
    /// Extract the PaymentId from the Location Header in case of 303 status code
    /// </summary>
    /// <param name="headersLocation">Location header value</param>
    /// <returns></returns>
    private static string ExtractPaymentIdFromHeader(string headersLocation) => headersLocation?.Split('/')[^1];

    /// <summary>
    /// Check if event is of type CDIPassedEvent or CDIFailedEvent with PayProvider as true.
    /// CDIFailedEvent with PayProvider as false is non-payable.
    /// </summary>
    /// <param name="cdiEvents"></param>
    /// <returns></returns>
    private static bool IsEventTypePayable(List<CdiEventForPayment> cdiEvents)
    {
        var isEventTypePayable =
            cdiEvents.Exists(e => e.EventType == nameof(CDIPassedEvent) || (e.EventType == nameof(CDIFailedEvent) && e.PayProvider!.Value));
        if (isEventTypePayable)
            cdiEvents.RemoveAll(e => e.EventType == nameof(CDIFailedEvent) && !e.PayProvider!.Value);
        return isEventTypePayable;
    }

    /// <summary>
    /// Invoke <see cref="ExamStatusEvent"/> as a Nsb event to write to database and optionally raise kafka event
    /// </summary>
    private Task PublishStatus(CdiEventForPayment cdiEvent, StatusCode statusCode, SpirometryExam exam, string reason = null, CancellationToken token = default)
    {
        var statusEvent = _mapper.Map<ExamStatusEvent>(cdiEvent);
        statusEvent.StatusCode = statusCode;
        statusEvent.Reason = reason;
        statusEvent.Exam = exam;
        return _mediator.Send(statusEvent, token);
    }

    /// <summary>
    /// Update ExamStatus table with the cdi event status
    /// </summary>
    private async Task AddNewCdiEventsToDatabase(IEnumerable<CdiEventForPayment> cdiEvents, SpirometryExam exam, CancellationToken token)
    {
        foreach (var cdiEvent in cdiEvents)
        {
            var statusCode = IdentifyStatusCode(cdiEvent);
            var status = new ExamStatusEvent
            {
                EventId = cdiEvent.RequestId,
                StatusCode = statusCode,
                Exam = exam,
                StatusDateTime = cdiEvent.DateTime.UtcDateTime
            };
            await _mediator.Send(status, token);
        }
    }

    /// <summary>
    /// Convert <see cref="CdiEventForPayment.EventType"/> of <see cref="CdiEventForPayment"/> to <see cref="StatusCode"/>
    /// </summary>
    /// <param name="cdiEvent"></param>
    /// <returns></returns>
    private static StatusCode IdentifyStatusCode(CdiEventForPayment cdiEvent)
    {
        if (cdiEvent.EventType == nameof(CDIPassedEvent))
        {
            return StatusCode.CdiPassedReceived;
        }

        return cdiEvent.PayProvider!.Value ? StatusCode.CdiFailedWithPayReceived : StatusCode.CdiFailedWithoutPayReceived;
    }

    /// <summary>
    /// Fetches the SpirometryExam based on EvaluationId
    /// </summary>
    /// <exception cref="ExamNotFoundException"></exception>
    private async Task<SpirometryExam> GetExam(long evaluationId, Guid eventId, CancellationToken token)
    {
        var exam = await _mediator.Send(new QuerySpirometryExam(evaluationId), token);
        if (exam is null)
        {
            throw new ExamNotFoundException(evaluationId, eventId);
        }

        return exam;
    }

    /// <summary>
    /// Commit both TransactionSupplier and Observability transactions
    /// </summary>
    /// <param name="transaction">The transaction generated as part of TransactionSupplier.BeginTransaction</param>
    /// <param name="token"></param>
    [Trace]
    private async Task CommitTransactions(IBufferedTransaction transaction, CancellationToken token)
    {
        await transaction.CommitAsync(token);
        _publishObservability.Commit();
    }

    #region Observability

    /// <summary>
    /// Publishes observability events
    /// </summary>
    /// <param name="exam"></param>
    /// <param name="eventId"></param>
    /// <param name="eventType"></param>
    /// <param name="eventParam"></param>
    /// <param name="eventParamValue"></param>
    /// <param name="sendImmediate"></param>
    /// <returns></returns>
    private void RegisterObservabilityEvent(SpirometryExam exam, Guid eventId, string eventType, string eventParam, object eventParamValue,
        bool sendImmediate = false)
    {
        try
        {
            var observabilityEvent = new ObservabilityEvent
            {
                EvaluationId = exam.EvaluationId,
                EventId = eventId,
                EventType = eventType,
                EventValue = new Dictionary<string, object>
                {
                    { Observability.EventParams.EvaluationId, exam.EvaluationId },
                    { eventParam, eventParamValue }
                }
            };

            _publishObservability.RegisterEvent(observabilityEvent, sendImmediate);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception while trying to add observability for EvaluationId={EvaluationId} EventId={EventId}",
                exam.EvaluationId, eventId);
        }
    }

    #endregion
}