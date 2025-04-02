using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.uACR.Core.Commands;
using Signify.uACR.Core.Constants;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using Signify.Dps.Observability.Library.Services;
using Signify.uACR.Core.Infrastructure;
using UacrNsbEvents;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public class ExamNotPerformedHandler(
    ILogger<ExamNotPerformedHandler> logger,
    ITransactionSupplier transactionSupplier,
    IMediator mediator,
    IPublishObservability publishObservability,
    IApplicationTime applicationTime)
    : BaseNsbHandler(logger, mediator, transactionSupplier, publishObservability, applicationTime),
        IHandleMessages<ExamNotPerformedEvent>
{
    [Transaction]
    public async Task Handle(ExamNotPerformedEvent message, IMessageHandlerContext context)
    {
        Logger.LogInformation("Start ExamNotPerformedEvent Handle for EventId={EventId}, EvaluationId={EvaluationId}", 
            message.EventId, message.Exam?.EvaluationId);
        
        using var transaction = TransactionSupplier.BeginTransaction();

        // Save the new exam to db
        var exam = await Mediator.Send(new AddExam(message.Exam), context.CancellationToken);

        // Save the reason the exam wasn't performed to db
        await Mediator.Send(new AddExamNotPerformed(exam, message.Reason, message.Notes), context.CancellationToken);

        await SendStatus(context, message, exam);

        await transaction.CommitAsync(context.CancellationToken);

        logger.LogInformation(
            "Finished handling evaluation where an exam was not performed, for EvaluationId={EvaluationId}, ExamId={ExamId}",
            exam.EvaluationId, exam.ExamId);
        
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