using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.DEE.Svc.Core.BusinessRules;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Events;
using Signify.DEE.Svc.Core.Infrastructure;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Models;
using Signify.DEE.Svc.Core.Messages.Queries;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Commands;

[ExcludeFromCodeCoverage]
public class ProcessPdfDelivered : ICommand
{
    public string EventId { get; set; }
    public long EvaluationId { get; set; }
    public DateTimeOffset DeliveryDateTime { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }
    public long BatchId { get; set; }
    public string BatchName { get; set; }

    public ProcessPdfDelivered(string eventId, long evaluationId, DateTimeOffset deliveryDateTime, DateTimeOffset createdDateTime, long batchId, string batchName)
    {
        EventId = eventId;
        EvaluationId = evaluationId;
        DeliveryDateTime = deliveryDateTime;
        CreatedDateTime = createdDateTime;
        BatchId = batchId;
        BatchName = batchName;
    }

    public ProcessPdfDelivered()
    {
    }

    protected bool Equals(ProcessPdfDelivered other)
        => EventId.Equals(other.EventId) && EvaluationId == other.EvaluationId &&
           DeliveryDateTime == other.DeliveryDateTime &&
           CreatedDateTime == other.CreatedDateTime && BatchId == other.BatchId && BatchName == other.BatchName;

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((ProcessPdfDelivered)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = EventId.GetHashCode();
            hashCode = (hashCode * 397) ^ EvaluationId.GetHashCode();
            hashCode = (hashCode * 397) ^ DeliveryDateTime.GetHashCode();
            hashCode = (hashCode * 397) ^ CreatedDateTime.GetHashCode();
            hashCode = (hashCode * 397) ^ BatchId.GetHashCode();
            hashCode = (hashCode * 397) ^ BatchName.GetHashCode();
            return hashCode;
        }
    }

    public override string ToString()
        => $"{nameof(EventId)}: {EventId}, {nameof(EvaluationId)}: {EvaluationId}, {nameof(DeliveryDateTime)}: {DeliveryDateTime}, {nameof(CreatedDateTime)}: {CreatedDateTime}, {nameof(BatchId)}: {BatchId}, {nameof(BatchName)}: {BatchName}";
}

public class ProcessPdfDeliveredHandler(
    ILogger<ProcessPdfDeliveredHandler> logger,
    IMediator mediator,
    IMapper mapper,
    ITransactionSupplier transactionSupplier,
    IBillableRules billableRules,
    IApplicationTime applicationTime)
    : IHandleMessages<ProcessPdfDelivered>
{
    [Transaction]
    public async Task Handle(ProcessPdfDelivered @event, IMessageHandlerContext context)
    {
        void Log(string message, LogLevel level = LogLevel.Information)
        {
            logger.Log(level, "{Message}, EvaluationID:{EvaluationId}, EventId:{EventId}", message, @event.EvaluationId, @event.EventId);
        }

        logger.LogDebug("Start Handle ProcessPdfDelivered, EvaluationID: {EvaluationId}", @event.EvaluationId);

        var exam = await mediator.Send(new GetExamByEvaluation { EvaluationId = @event.EvaluationId }, context.CancellationToken);

        if (exam == default)
        {
            Log("Evaluation not found in DB", LogLevel.Error);
            return;
        }

        var pdfEntry = await mediator.Send(new GetPdfToClient(exam.ExamId, exam.EvaluationId), context.CancellationToken);

        using var transaction = transactionSupplier.BeginTransaction();

        if (pdfEntry.EvaluationId == 0)
        {
            await CreatePdfDelivery(@event, exam);
        }
        else
        {
            Log("PDF delivery entry exists in the DB");
        }

        var statusCodes = await GetStatusCodesForExam(exam.ExamId);
        var isIncompleteStatusPresent = billableRules.IsIncompleteStatusPresent(new BillableRuleAnswers { StatusCodes = statusCodes }).IsMet;

        var notGradable = billableRules.IsNotGradable(new BillableRuleAnswers { StatusCodes = statusCodes }).IsMet;
        if (notGradable)
        {
            logger.LogInformation("Image exam is not gradable, not sending RCM Billing Request");
        }

        // If Status Code is Incomplete then do not send billing request to RCM // Log Error
        if (isIncompleteStatusPresent || notGradable)
        {
            await PublishBillRequestNotSent(exam, @event.DeliveryDateTime);

            if (isIncompleteStatusPresent)
                logger.LogInformation("ExamId: {ExamId} has Incomplete Image Status so no billing request will be sent to RCM", exam.ExamId);
            if (notGradable)
                logger.LogInformation("ExamId: {ExamId} is Not Gradable so no billing request will be sent to RCM", exam.ExamId);

            await transaction.CommitAsync(context.CancellationToken);
            logger.LogDebug("End Handle PdfDeliveredHandler");
            return;
        }

        var gradable = billableRules.IsGradable(new BillableRuleAnswers { StatusCodes = statusCodes }).IsMet;
        if (gradable)
        {
            var billId = await mediator.Send(new GetRcmBillId(exam.ExamId), context.CancellationToken);
            if (string.IsNullOrEmpty(billId)) await SendBillingRequest(@event, exam, context);
        }

        await transaction.CommitAsync(context.CancellationToken);

        logger.LogDebug("End Handle ProcessPdfDelivered");
    }

    private async Task PublishBillRequestNotSent(Exam exam, DateTimeOffset deliveryDateTime)
    {
        var now = applicationTime.UtcNow();

        var statusAddedResponse = await mediator.Send(new CreateStatus(exam.ExamId, ExamStatusCode.BillRequestNotSent.Name, now));
        if (statusAddedResponse.IsNew)
        {
            var status = mapper.Map<DEE.Messages.Status.BillRequestNotSent>(exam);

            status.PdfDeliveryDate = deliveryDateTime;
            status.ReceivedDate = now;

            await mediator.Send(new PublishStatusUpdate(status));
        }
    }

    private async Task CreatePdfDelivery(ProcessPdfDelivered @event, Exam exam)
    {
        var pdfDeliveryReceived = new CreateOrUpdatePdfToClient
        {
            EventId = @event.EventId,
            EvaluationId = @event.EvaluationId,
            DeliveryDateTime = @event.DeliveryDateTime,
            DeliveryCreatedDateTime = @event.CreatedDateTime,
            BatchId = @event.BatchId,
            BatchName = @event.BatchName,
            ExamId = exam.ExamId
        };

        await mediator.Send(pdfDeliveryReceived);

        // update status
        await mediator.Send(new CreateStatus(exam.ExamId, ExamStatusCode.BillableEventRecieved.Name, exam.CreatedDateTime));

        logger.LogInformation("PDF Delivery Entry Saved for ExamId: {ExamId}.", exam.ExamId);
    }

    private async Task SendBillingRequest(ProcessPdfDelivered eventMessage, Exam exam, IMessageHandlerContext context)
    {
        var rcmBillingRequest = mapper.Map<RCMBillingRequestEvent>(exam);
        rcmBillingRequest.ApplicationId = "signify.dee.service";
        rcmBillingRequest.BillableDate = eventMessage.DeliveryDateTime;
        rcmBillingRequest.SharedClientId = exam.ClientId ?? 0;
        rcmBillingRequest.CorrelationId = Guid.NewGuid().ToString();
        rcmBillingRequest.AdditionalDetails = new Dictionary<string, string>
        {
            { "BatchName", eventMessage.BatchName },
            { "EvaluationId", rcmBillingRequest.EvaluationId.ToString() }
        };
        if (exam.AppointmentId != null)
        {
            rcmBillingRequest.AdditionalDetails.Add("appointmentId", exam.AppointmentId.ToString());
        }

        await context.SendLocal(rcmBillingRequest);
        logger.LogInformation("RCMBillingRequest sent successfully from PdfDeliveredHandler");
    }

    /// <summary>
    /// Fetches the status codes associated with the examId
    /// </summary>
    /// <param name="examId"></param>
    /// <returns></returns>
    private Task<List<int>> GetStatusCodesForExam(int examId)
        => mediator.Send(new GetAllExamStatus(examId));
}