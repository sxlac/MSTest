using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.uACR.Core.Commands;
using Signify.uACR.Core.Constants;
using Signify.uACR.Core.Queries;
using System.Threading.Tasks;
using System.Threading;
using System;
using Signify.Dps.Observability.Library.Services;
using Signify.uACR.Core.Data;
using Signify.uACR.Core.Infrastructure;
using UacrNsbEvents;
using Task = System.Threading.Tasks.Task;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

/// <summary>
/// Event handler that coordinates what to do when this process manager receives an evaluation over Kafka
/// that contains a uACR product.
/// </summary>
public class EvalReceivedHandler(
    ILogger<EvalReceivedHandler> logger,
    IMediator mediator,
    ITransactionSupplier transactionSupplier,
    IPublishObservability publishObservability,
    IApplicationTime applicationTime)
    : BaseNsbHandler(logger, mediator, transactionSupplier, publishObservability, applicationTime), IHandleMessages<EvalReceived>
{
    [Transaction]
    public async Task Handle(EvalReceived message, IMessageHandlerContext context)
    {
        Logger.LogInformation("Start EvalReceivedHandler  for EvaluationId={EvaluationId}", message.EvaluationId);
       
        try
        {
            if (await ExamExistsInDatabase(message, context.CancellationToken).ConfigureAwait(false))
            {
                await HandleUpdatedEvaluation(message, context.CancellationToken).ConfigureAwait(false);
            }
            else
            {
                await HandleNewEvaluation(message, context).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error Message: {Message} - For EvaluationId={EvaluationId}", ex.Message, message.EvaluationId);
            throw;
        }
        finally
        {
            Logger.LogInformation("End EvalReceivedHandler for EvaluationId={EvaluationId}", message.EvaluationId);
        }
    }

    private async Task<bool> ExamExistsInDatabase(EvalReceived message, CancellationToken token)
    {
        var exists = await Mediator.Send(new QueryExamByEvaluation { EvaluationId = message.EvaluationId }, token) != null;

        Logger.LogInformation("Exam {ExistCheck} exist in database for EvaluationId={EvaluationId}", exists ? "does" : "does not", message.EvaluationId);
        return exists;
    }
    private async Task HandleUpdatedEvaluation(EvalReceived message, CancellationToken token)
    {
        await Mediator.Send(new UpdateExam(message), token).ConfigureAwait(false);
            
        PublishObservabilityEvents(message.EvaluationId, message.CreatedDateTime,
            Observability.Evaluation.EvaluationClarificationEvent, null, true);
    }
    
     private async Task HandleNewEvaluation(EvalReceived message, IPipelineContext context)
        {
            // Query Evaluation API for the evaluation answers, and create the ExamModel
            var examModel = await Mediator.Send(new GenerateExamModel(message.EvaluationId), context.CancellationToken).ConfigureAwait(false);
            
            // Aggregate info from EvalReceived, Provider info, and Member info
            var exam = await Mediator.Send(new AggregateExamDetails(message), context.CancellationToken).ConfigureAwait(false);

            if (!examModel.NotPerformedReason.HasValue)
            {
                await context.SendLocal(new ExamPerformedEvent
                {
                    EventId = message.Id,
                    Exam = exam,
                    Result = examModel.ExamResult
                });
            }
            else
            {
                await context.SendLocal(new ExamNotPerformedEvent
                {
                    EventId = message.Id,
                    Exam = exam,
                    Reason = examModel.NotPerformedReason.Value,
                    Notes = examModel.Notes
                });
            }

            Logger.LogInformation(
                "Event queued for processing an evaluation where an exam {Performed} performed, for EvaluationId={EvaluationId}, ExamId={ExamId}",
                examModel.ExamPerformed ? "was" : "was not",
                exam.EvaluationId, exam.ExamId);
            PublishObservabilityEvents(message.EvaluationId, message.CreatedDateTime,
                Observability.Evaluation.EvaluationReceivedEvent, null, true);
        }
}