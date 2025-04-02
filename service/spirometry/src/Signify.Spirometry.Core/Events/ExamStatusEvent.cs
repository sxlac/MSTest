using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Constants;
using Signify.Spirometry.Core.Constants.Questions.NotPerformed;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Events.Status;
using Signify.Spirometry.Core.Queries;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using NewRelic.Api.Agent;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using StatusCode = Signify.Spirometry.Core.Models.StatusCode;

namespace Signify.Spirometry.Core.Events;

[ExcludeFromCodeCoverage]
public class ExamStatusEvent : IRequest
{
    public Guid EventId { get; set; }

    public SpirometryExam Exam { get; set; }

    public StatusCode StatusCode { get; set; }

    /// <summary>
    /// The date and time when this status change event occurred.
    /// i.e the datetime contained within the incoming Kafka event
    /// </summary>
    public DateTime StatusDateTime { get; set; }

    /// <summary>
    /// Name of the CDI event that triggered ProviderPay
    /// </summary>
    public string ParentCdiEvent { get; set; }

    /// <summary>
    /// Unique identifier returned by ProviderPay API
    /// </summary>
    public string PaymentId { get; set; }

    /// <summary>
    /// Reason why payment is not be done, if applicable
    /// </summary>
    public string Reason { get; set; }

    /// <summary>
    /// Date and time the Kafka event was received by the PM
    /// </summary>
    public DateTimeOffset ParentEventReceivedDateTime { get; set; }
}

public class ExamStatusEventHandler : IRequestHandler<ExamStatusEvent>
{
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly ILogger<ExamStatusEventHandler> _logger;
    private readonly IPublishObservability _publishObservabilityEvents;

    public ExamStatusEventHandler(ILogger<ExamStatusEventHandler> logger, IMapper mapper, IMediator mediator,
        IPublishObservability publishObservabilityEvents)
    {
        _mapper = mapper;
        _mediator = mediator;
        _publishObservabilityEvents = publishObservabilityEvents;
        _logger = logger;
    }

    [Transaction]
    public async Task Handle(ExamStatusEvent message, CancellationToken cancellationToken)
    {
        await SaveStatusToDb(message, cancellationToken);

        var eventToPublish = await CreateKafkaStatusEvent(message, cancellationToken);

        if (eventToPublish != null)
            await PublishStatusEvent(message.EventId, eventToPublish, cancellationToken);

        PublishObservabilityEvents(message);
    }

    #region Kafka

    private Task PublishStatusEvent(Guid eventId, BaseStatusMessage @event, CancellationToken token)
    {
        return _mediator.Send(new PublishStatusUpdate(eventId, @event), token);
    }

    private async Task<BaseStatusMessage> CreateKafkaStatusEvent(ExamStatusEvent message, CancellationToken token)
    {
        switch (message.StatusCode)
        {
            case StatusCode.SpirometryExamPerformed:
                return _mapper.Map<Performed>(message.Exam);
            case StatusCode.SpirometryExamNotPerformed:
                var notPerformed = _mapper.Map<NotPerformed>(message.Exam);
                var examNotPerformedEntity = await _mediator.Send(new QueryExamNotPerformed(message.Exam.EvaluationId), token);
                _mapper.Map(examNotPerformedEntity, notPerformed);
                notPerformed.ReasonType = GetReasonType(examNotPerformedEntity.NotPerformedReason.AnswerId);
                return notPerformed;
            case StatusCode.BillRequestSent:
                var billRequestSent = _mapper.Map<Status.BillRequestSent>(message.Exam);
                var billRequestEntity = (await _mediator.Send(new QueryBillRequestSent(message.Exam.EvaluationId), token)).Entity;
                _mapper.Map(billRequestEntity, billRequestSent);
                var pdfDeliveryBillEntity = (await _mediator.Send(new QueryPdfDeliveredToClient(message.Exam.EvaluationId), token)).Entity;
                _mapper.Map(pdfDeliveryBillEntity, billRequestSent);
                return billRequestSent;
            case StatusCode.BillRequestNotSent:
                var billRequestNotSent = _mapper.Map<BillRequestNotSent>(message.Exam);
                var pdfDeliveryEntity = (await _mediator.Send(new QueryPdfDeliveredToClient(message.Exam.EvaluationId), token)).Entity;
                _mapper.Map(pdfDeliveryEntity, billRequestNotSent);
                return billRequestNotSent;
            case StatusCode.ResultsReceived:
                var resultsReceived = _mapper.Map<ResultsReceived>(message.Exam);

                // ReceivedDate gets mapped from the SpirometryExam, with the exception of when results
                // correspond to an overread, in which case the ReceivedDate is the date at which the
                // overread was received from the vendor.
                var isOverreadResult = await HasExamStatus(message.Exam, Data.Entities.StatusCode.OverreadProcessed, token);
                if (!isOverreadResult)
                    return resultsReceived;

                // Yes, you could just query this instead of the status from the db, but we don't want
                // to use the overread received date if the overread was not actually processed. This
                // covers an edge case where we receive overread results for a clinically-valid (ie A/B/C)
                // POC test, so we correctly use the received date from the exam and not the overread.
                var overreadResult = await _mediator.Send(new QueryOverreadResult(message.Exam.AppointmentId), token);
                resultsReceived.ReceivedDate = overreadResult.ReceivedDateTime;

                return resultsReceived;
            case StatusCode.ProviderPayRequestSent:
                var providerPayRequestEvent = _mapper.Map<ProviderPayRequestSent>(message.Exam);
                _mapper.Map(message, providerPayRequestEvent);
                return providerPayRequestEvent;
            case StatusCode.ProviderPayableEventReceived:
                var providerPayableEventReceived = _mapper.Map<ProviderPayableEventReceived>(message.Exam);
                _mapper.Map(message, providerPayableEventReceived);
                return providerPayableEventReceived;
            case StatusCode.ProviderNonPayableEventReceived:
                var providerNonPayableEventReceived = _mapper.Map<ProviderNonPayableEventReceived>(message.Exam);
                _mapper.Map(message, providerNonPayableEventReceived);
                return providerNonPayableEventReceived;
            case StatusCode.ClarificationFlagCreated:
                return _mapper.Map<FlaggedForLoopback>(message.Exam);
            case StatusCode.BillableEventReceived:
            case StatusCode.ClientPdfDelivered:
            case StatusCode.OverreadProcessed:
            case StatusCode.CdiPassedReceived:
            case StatusCode.CdiFailedWithPayReceived:
            case StatusCode.CdiFailedWithoutPayReceived:
                return null; // None of these are events that need to be published to Kafka
            default:
                throw new NotImplementedException($"Status code {message.StatusCode} has not been handled");
        }
    }

    private async Task<bool> HasExamStatus(SpirometryExam exam, Data.Entities.StatusCode statusCode, CancellationToken token)
    {
        return await _mediator.Send(new QueryExamStatus
        {
            SpirometryExamId = exam.SpirometryExamId,
            StatusCode = statusCode
        }, token) != null;
    }

    private string GetReasonType(int answerId)
    {
        switch (answerId)
        {
            case ReasonMemberRefusedQuestion.MemberRecentlyCompletedAnswerId:
            case ReasonMemberRefusedQuestion.ScheduledToCompleteAnswerId:
            case ReasonMemberRefusedQuestion.MemberApprehensionAnswerId:
            case ReasonMemberRefusedQuestion.NotInterestedAnswerId:
                return ReasonType.MemberRefusal;
            case ReasonUnableToPerformQuestion.TechnicalIssueAnswerId:
            case ReasonUnableToPerformQuestion.EnvironmentalIssueAnswerId:
            case ReasonUnableToPerformQuestion.NoSuppliesOrEquipmentAnswerId:
            case ReasonUnableToPerformQuestion.InsufficientTrainingAnswerId:
            case ReasonUnableToPerformQuestion.MemberPhysicallyUnableAnswerId:
            case ReasonUnableToPerformQuestion.MemberOutsideDemographicRangesAnswerId:
                return ReasonType.MemberUnableToPerform;
            default:
                _logger.LogWarning("Unable to match AnswerId={AnswerId} with set Member Refusal and Unable to Perform reason types", answerId);
                return string.Empty;
        }
    }

    #endregion

    #region Database

    private Task SaveStatusToDb(ExamStatusEvent message, CancellationToken token)
    {
        var alwaysAddStatus = IsMultipleStatusAllowed(message.StatusCode);
        var status = _mapper.Map<ExamStatus>(message);
        return _mediator.Send(new AddExamStatus(message.EventId, message.Exam.EvaluationId, status, alwaysAddStatus), token);
    }

    /// <summary>
    /// Determine if multiple status codes are allowed
    /// </summary>
    /// <param name="statusCode"></param>
    /// <returns></returns>
    private static bool IsMultipleStatusAllowed(StatusCode statusCode)
    {
        switch (statusCode)
        {
            case StatusCode.CdiPassedReceived:
            case StatusCode.CdiFailedWithPayReceived:
            case StatusCode.CdiFailedWithoutPayReceived:
            case StatusCode.ProviderPayableEventReceived:
            case StatusCode.ProviderNonPayableEventReceived:
                return true;
            case StatusCode.SpirometryExamPerformed:
            case StatusCode.SpirometryExamNotPerformed:
            case StatusCode.BillableEventReceived:
            case StatusCode.BillRequestSent:
            case StatusCode.ClientPdfDelivered:
            case StatusCode.BillRequestNotSent:
            case StatusCode.OverreadProcessed:
            case StatusCode.ResultsReceived:
            case StatusCode.ClarificationFlagCreated:
            case StatusCode.ProviderPayRequestSent:
                return false;
            default:
                throw new NotImplementedException($"Status code {statusCode} has not been handled");
        }
    }

    #endregion

    #region Observability

    /// <summary>
    /// Method to emit events to surface in observability dashboard.
    /// Events are just added here but are committed when the transaction is committed.
    /// </summary>
    /// <param name="message"></param>
    [Trace]
    private void PublishObservabilityEvents(ExamStatusEvent message)
    {
        switch (message.StatusCode)
        {
            case StatusCode.CdiPassedReceived:
            case StatusCode.CdiFailedWithPayReceived:
            case StatusCode.CdiFailedWithoutPayReceived:
                Publish(Observability.ProviderPay.CdiEvents, Observability.EventParams.CdiEvent);
                return;
            case StatusCode.ProviderPayableEventReceived:
                Publish(Observability.ProviderPay.PayableCdiEvents, Observability.EventParams.PayableCdiEvents);
                return;
            case StatusCode.ProviderNonPayableEventReceived:
                Publish(Observability.ProviderPay.NonPayableCdiEvents,
                    Observability.EventParams.NonPayableCdiEvents,
                    new Dictionary<string, object>
                    {
                        { Observability.EventParams.NonPayableReason, message.Reason }
                    });
                return;
            case StatusCode.ProviderPayRequestSent:
                Publish(Observability.ProviderPay.PayableCdiEvents,
                    Observability.EventParams.PayableCdiEvents,
                    new Dictionary<string, object>
                    {
                        { Observability.EventParams.PaymentId, message.PaymentId }
                    });
                return;
            default:
                return;
        }

        void Publish(string eventType, string eventStatusParam, Dictionary<string, object> additionalEventDetails = null)
        {
            var observabilityEvent = new ObservabilityEvent
            {
                EvaluationId = message.Exam.EvaluationId,
                EventId = message.EventId,
                EventType = eventType,
                EventValue = new Dictionary<string, object>
                {
                    { Observability.EventParams.EvaluationId, message.Exam.EvaluationId },
                    { eventStatusParam, message.StatusCode.ToString() }
                }
            };
            if (additionalEventDetails is not null)
            {
                foreach (var detail in additionalEventDetails)
                {
                    observabilityEvent.EventValue.TryAdd(detail.Key, detail.Value);
                }
            }

            _publishObservabilityEvents.RegisterEvent(observabilityEvent);
        }
    }

    #endregion
}