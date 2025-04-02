using EgfrNsbEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Constants;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using Signify.Dps.Observability.Library.Services;
using Signify.eGFR.Core.Infrastructure;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public class ExamNotPerformedHandler(
    ILogger<ExamNotPerformedHandler> logger,
    IMediator mediator,
    ITransactionSupplier transactionSupplier,
    IPublishObservability publishObservability,
    IApplicationTime applicationTime)
    : BaseNsbHandler(logger, mediator, transactionSupplier, publishObservability, applicationTime), IHandleMessages<ExamNotPerformedEvent>
{
    [Transaction]
    public async Task Handle(ExamNotPerformedEvent message, IMessageHandlerContext context)
    {
        Logger.LogDebug(
            "Started handling evaluation where an exam was not performed, EventId={EventId}, EvaluationId={EvaluationId}",
            message.EventId, message.Exam.EvaluationId);

        using var transaction = TransactionSupplier.BeginTransaction();

        // Save the new exam to db
        var exam = await Mediator.Send(new AddExam(message.Exam), context.CancellationToken);

        //TODO: Add error handling here to handle an edge case:
        // If insert fails due to EvaluationId already exists in db, then this event is being processed from
        // an NSB error queue. After it was placed on the error queue, and before now, we received another
        // EvaluationFinalized event for this evaluation that was in fact an update (ex date of service was updated),
        // but that event was treated as the first time we've seen this evaluation since it wasn't yet in our db,
        // so it was inserted before this current event was finished processing from the error queue.

        // Save the reason the exam wasn't performed to db
        await Mediator.Send(new AddExamNotPerformed(exam, message.Reason, message.Notes), context.CancellationToken);

        await SendStatus(context, message, exam);
        await transaction.CommitAsync(context.CancellationToken);

        Logger.LogInformation(
            "Finished handling evaluation where an exam was not performed, EventId={EventId}, EvaluationId={EvaluationId}",
            message.EventId, message.Exam.EvaluationId);

        PublishObservabilityEvents(exam.EvaluationId, exam.CreatedDateTime,
            Observability.Evaluation.EvaluationNotPerformedEvent,
            new Dictionary<string, object> { { Observability.EventParams.NotPerformedReason, message.Reason.ToString() } }, true);
    }

    private static async Task SendStatus(IPipelineContext context, ExamNotPerformedEvent message, Exam exam)
    {
        await context.SendLocal(new ExamStatusEvent
        {
            EventId = message.EventId,
            EvaluationId = exam.EvaluationId,
            ExamId = exam.ExamId,
            StatusCode = ExamStatusCode.ExamNotPerformed,
            StatusDateTime = message.Exam.EvaluationReceivedDateTime
        });
    }
}