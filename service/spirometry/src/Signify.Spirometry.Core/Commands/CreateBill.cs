using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Refit;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.Spirometry.Core.ApiClients.RcmApi;
using Signify.Spirometry.Core.ApiClients.RcmApi.Requests;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Exceptions;
using Signify.Spirometry.Core.Infrastructure;
using Signify.Spirometry.Core.Queries;
using Signify.Spirometry.Core.Constants;

namespace Signify.Spirometry.Core.Commands;

/// <summary>
/// Command to send a new billing request to RCM
/// </summary>
public class CreateBill : IRequest<BillRequestSent>
{
    /// <summary>
    /// Identifier of the event that was billable
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Identifier of the evaluation this bill corresponds to
    /// </summary>
    public long EvaluationId { get; set; }

    /// <summary>
    /// Timestamp of when the event was billable
    /// </summary>
    public DateTime BillableDate { get; set; }

    public string BatchName { get; set; }
}

public class CreateBillHandler : IRequestHandler<CreateBill, BillRequestSent>
{
    private readonly ILogger _logger;
    private readonly IApplicationTime _applicationTime;
    private readonly IRcmApi _rcmApi;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly IPublishObservability _publishObservability;

    public CreateBillHandler(ILogger<CreateBillHandler> logger,
        IApplicationTime applicationTime,
        IRcmApi rcmApi,
        IMediator mediator,
        IMapper mapper, IPublishObservability publishObservability)
    {
        _logger = logger;
        _applicationTime = applicationTime;
        _rcmApi = rcmApi;
        _mediator = mediator;
        _mapper = mapper;
        _publishObservability = publishObservability;
    }

    public async Task<BillRequestSent> Handle(CreateBill request, CancellationToken cancellationToken)
    {
        var exam = await _mediator.Send(new QuerySpirometryExam(request.EvaluationId), cancellationToken);

        var createBillRequest = _mapper.Map<CreateBillRequest>(exam);
        _mapper.Map(request, createBillRequest);

        createBillRequest.CorrelationId = GenerateCorrelationId();

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Sending billing request to RCM API for EvaluationId={EvaluationId} with CorrelationId={CorrelationId}: Request={Request}",
                request.EvaluationId, createBillRequest.CorrelationId, JsonConvert.SerializeObject(createBillRequest));
        }
        else
        {
            _logger.LogInformation("Sending billing request to RCM API for EvaluationId={EvaluationId} with CorrelationId={CorrelationId}",
                request.EvaluationId, createBillRequest.CorrelationId);
        }

        var response = await _rcmApi.SendBillingRequest(createBillRequest);

        RegisterObservabilityEvent(exam, request.EventId, Observability.ProviderPay.ProviderPayApiStatusCodeEvent, Observability.EventParams.StatusCode,
            response?.StatusCode, true);
        RegisterObservabilityEvent(exam, request.EventId, Observability.ProviderPay.ProviderPayOrBillingEvent, Observability.EventParams.Type,
            Observability.EventParams.TypeProviderPay);

        ValidateSuccessful(request, response);

        var billId = GetBillId(request, response);

        PublishSuccessObservabilityEvent(request, Observability.RcmBilling.BillRequestRaisedEvent, billId.ToString());
        
        var addCommand = new AddOrUpdateBillRequestSent(request.EventId, request.EvaluationId, new BillRequestSent
        {
            BillId = billId,
            SpirometryExamId = exam.SpirometryExamId,
            CreatedDateTime = _applicationTime.UtcNow()
        });

        return await _mediator.Send(addCommand, cancellationToken);
    }

    private void ValidateSuccessful(CreateBill request, IApiResponse response)
    {
        // See: https://wiki.signifyhealth.com/display/BILL/Integration+Guide

        // Response codes returned by API:
        // Accepted (202)
        // Moved Permanently (301)
        // Bad Request (400)

        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.MovedPermanently)
        {
            _logger.LogInformation("Received StatusCode={StatusCode} from RCM API, for EvaluationId={EvaluationId}", response.StatusCode,
                request.EvaluationId);
            return;
        }

        if (response.StatusCode is >= HttpStatusCode.BadRequest and < HttpStatusCode.InternalServerError)
            _logger.LogWarning("Received StatusCode={StatusCode} from RCM API for EvaluationId={EvaluationId} with Request={Request}", response.StatusCode,
                request.EvaluationId, JsonConvert.SerializeObject(request));

        var message = "Unsuccessful HTTP status code returned";
        if (!string.IsNullOrEmpty(response.Error?.Content))
            message =
                $"{message}, with response: {response.Error.Content}"; // For 400, RCM includes the failure reason in the response content; see https://wiki.signifyhealth.com/display/BILL/Integration+Guide

        PublishFailedObservabilityEvent(request, Observability.RcmBilling.BillRequestFailedEvent);
        
        // Raise for NSB retry
        throw new RcmBillingRequestException(request.EventId, request.EvaluationId, response.StatusCode,
            message, response.Error);
    }

    private static Guid GetBillId(CreateBill request, IApiResponse<Guid?> response)
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
        throw new RcmBillingRequestException(request.EventId, request.EvaluationId, response.StatusCode,
            "BillId was not included in the API response");
    }

    private static string GenerateCorrelationId()
    {
        return Guid.NewGuid().ToString();
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

    private void PublishFailedObservabilityEvent(CreateBill request, string eventType)
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
    private void PublishSuccessObservabilityEvent(CreateBill request, string eventType, string billId)
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
    #endregion
}