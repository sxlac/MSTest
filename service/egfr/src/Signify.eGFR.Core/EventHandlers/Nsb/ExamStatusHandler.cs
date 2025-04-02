using AutoMapper;
using EgfrNsbEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Constants.Questions;
using Signify.eGFR.Core.Constants;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Events.Status;
using Signify.eGFR.Core.Exceptions;
using Signify.eGFR.Core.Models;
using Signify.eGFR.Core.Queries;
using System.Threading.Tasks;
using System.Threading;
using System;

using BillRequestSent = Signify.eGFR.Core.Events.Status.BillRequestSent;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public class ExamStatusHandler(
    ILogger<ExamStatusHandler> logger,
    IMapper mapper,
    IMediator mediator,
    ITransactionSupplier transactionSupplier,
    IAgent newRelicAgent)
    : IHandleMessages<ExamStatusEvent>
{
    [Transaction]
    public async Task Handle(ExamStatusEvent message, IMessageHandlerContext context)
    {
        logger.LogDebug("Start {Handle} for EvaluationId={EvaluationId}", nameof(ExamStatusHandler), message.EvaluationId);

        using var transaction = transactionSupplier.BeginTransaction();
        var exam = await GetExam(message.EvaluationId, context.CancellationToken);

        await SaveStatusToDb(message, exam, context.CancellationToken);

        var eventToPublish = await CreateKafkaStatusEvent(message.StatusCode, exam, message, CancellationToken.None);

        if (eventToPublish is not null)
        {
            await PublishStatusToKafka(message.EventId, eventToPublish, context.CancellationToken);
        }

        await transaction.CommitAsync(context.CancellationToken);

        EmitNewRelicAttributes(message.StatusCode);
        logger.LogDebug("End {Handle} for EvaluationId={EvaluationId}", nameof(ExamStatusHandler), message.EvaluationId);
    }

    private async Task<Exam> GetExam(long evaluationId, CancellationToken token)
    {
        return await mediator.Send(new QueryExam(evaluationId), token);
    }

    private async Task SaveStatusToDb(ExamStatusEvent message, Exam exam, CancellationToken token)
    {
        var status = mapper.Map<ExamStatus>(message);

        // Some handlers that raise a status event are only aware of EvaluationID
        if (status.ExamId < 1)
        {
            status.ExamId = exam.ExamId;
        }

        await mediator.Send(new AddExamStatus(message.EventId, message.EvaluationId, status), token);
    }

    /// <summary>
    /// Create the event type based on the status code
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="exam"></param>
    /// <param name="message"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task<BaseStatusMessage> CreateKafkaStatusEvent(ExamStatusCode statusCode, Exam exam, ExamStatusEvent message, CancellationToken token)
    {
        switch (statusCode.StatusCodeId)
        {
            case (int)StatusCode.ExamPerformed:
                var statusEvent = mapper.Map<Performed>(exam);
                statusEvent.Barcode = message.Barcode;
                return statusEvent;
            case (int)StatusCode.ExamNotPerformed:
                var notPerformed = mapper.Map<NotPerformed>(exam);
                var examNotPerformedEntity = await mediator.Send(new QueryExamNotPerformed(exam.EvaluationId), token);
                mapper.Map(examNotPerformedEntity, notPerformed);
                notPerformed.ReasonType = GetReasonType(examNotPerformedEntity.NotPerformedReason.AnswerId);
                return notPerformed;
            case (int)StatusCode.BillRequestSent:
                var billRequestSentMessage = (BillableExamStatusEvent) message;
                var billRequestSent = mapper.Map<BillRequestSent>(exam);
                billRequestSent.BillId = billRequestSentMessage.BillId == Guid.Empty 
                    ? throw new RcmBillIdException(message.EventId, message.EvaluationId, "BillId is empty")
                    : billRequestSentMessage.BillId;
                billRequestSent.PdfDeliveryDate = billRequestSentMessage.PdfDeliveryDateTime;
                billRequestSent.BillingProductCode = message.RcmProductCode.ToUpper();
                return billRequestSent; 
            case (int)StatusCode.BillRequestNotSent:
                var billRequestNotSentMessage = (BillableExamStatusEvent) message;
                var billRequestNotSent = mapper.Map<BillRequestNotSent>(exam);
                billRequestNotSent.PdfDeliveryDate = billRequestNotSentMessage.PdfDeliveryDateTime;
                billRequestNotSent.BillingProductCode = message.RcmProductCode.ToUpper();
                return billRequestNotSent;
            case (int)StatusCode.BillableEventReceived:
            case (int)StatusCode.ClientPdfDelivered:
                return null; // None of these are events that need to be published to Kafka
            default:
                throw new NotImplementedException($"Status code {nameof(statusCode)} has not been handled");
        }
    }

    private async Task PublishStatusToKafka(Guid eventId, BaseStatusMessage eventToPublish, CancellationToken token)
    {
        await mediator.Send(new PublishStatusUpdate(eventId, eventToPublish), token);
    }

    private string GetReasonType(int answerId)
    {
        switch (answerId)
        {
            case ReasonMemberRefusedQuestion.MemberRecentlyCompletedAnswerId:
            case ReasonMemberRefusedQuestion.ScheduledToCompleteAnswerId:
            case ReasonMemberRefusedQuestion.MemberApprehensionAnswerId:
            case ReasonMemberRefusedQuestion.NotInterestedAnswerId:
            case KedReasonMemberRefusedQuestion.ScheduledToCompleteAnswerId:
            case KedReasonMemberRefusedQuestion.MemberApprehensionAnswerId:
            case KedReasonMemberRefusedQuestion.NotInterestedAnswerId:
            case KedReasonMemberRefusedQuestion.MemberRecentlyCompletedAnswerId:
                return ReasonType.MemberRefusal;
            case ReasonProviderUnableToPerformQuestion.TechnicalIssueAnswerId:
            case ReasonProviderUnableToPerformQuestion.EnvironmentalIssueAnswerId:
            case ReasonProviderUnableToPerformQuestion.NoSuppliesOrEquipmentAnswerId:
            case ReasonProviderUnableToPerformQuestion.InsufficientTrainingAnswerId:
            case ReasonProviderUnableToPerformQuestion.MemberPhysicallyUnableAnswerId:
            case KedReasonProviderUnableToPerformQuestion.TechnicalIssueAnswerId:
            case KedReasonProviderUnableToPerformQuestion.EnvironmentalIssueAnswerId:
            case KedReasonProviderUnableToPerformQuestion.NoSuppliesOrEquipmentAnswerId:
            case KedReasonProviderUnableToPerformQuestion.InsufficientTrainingAnswerId:
            case KedReasonProviderUnableToPerformQuestion.MemberPhysicallyUnableAnswerId:
                return ReasonType.MemberUnableToPerform;
            case ReasonNotPerformedQuestion.ClinicallyNotRelevant:
                return ReasonType.ClinicallyNotRelevant;
            default:
                logger.LogWarning("Unable to match AnswerId={AnswerId} with set Member Refusal and Unable to Perform reason types", answerId);
                return string.Empty;
        }
    }

    /// <summary>
    /// Method to emit New Relic Custom Attributes.
    /// Calling the method at the end of the Handler so that multiple events are not emitted in case of errors and NSB retries.
    /// </summary>
    /// <param name="status"></param>
    [Trace]
    private void EmitNewRelicAttributes(ExamStatusCode status)
    {
        var currentTransaction = newRelicAgent.CurrentTransaction;
        switch (status.StatusCodeId)
        {
            case (int)StatusCode.ExamPerformed:
                currentTransaction.AddCustomAttribute("PerformedOrNot", true);
                break;
            case (int)StatusCode.ExamNotPerformed:
                currentTransaction.AddCustomAttribute("PerformedOrNot", false);
                break;
            case (int)StatusCode.BillableEventReceived:
            case (int)StatusCode.BillRequestSent:
            case (int)StatusCode.ClientPdfDelivered:
                break;
        }
    }
}