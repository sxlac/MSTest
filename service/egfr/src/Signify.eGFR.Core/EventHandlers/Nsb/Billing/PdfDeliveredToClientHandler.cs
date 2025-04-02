using EgfrNsbEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.eGFR.Core.BusinessRules;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Constants;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Models;
using Signify.eGFR.Core.Queries;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.eGFR.Core.Events.Akka;
using Signify.eGFR.Core.Exceptions;
using Signify.eGFR.Core.FeatureFlagging;
using Signify.eGFR.Core.Infrastructure;
using PdfDeliveredToClient = EgfrEvents.PdfDeliveredToClient;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

/// <summary>
/// NSB event handler for the <see cref="EgfrEvents.PdfDeliveredToClient"/>
/// </summary>
public class PdfDeliveredToClientHandler(
    ILogger<PdfDeliveredToClientHandler> logger,
    IMediator mediator,
    ITransactionSupplier transactionSupplier,
    IPublishObservability publishObservability,
    IApplicationTime applicationTime,
    IBillableRules billableRules,
    IMapper mapper,
    IFeatureFlags featureFlags)
    : BaseNsbHandler(logger, mediator, transactionSupplier, publishObservability, applicationTime), IHandleMessages<PdfDeliveredToClient>
{

    [Transaction]
    public async Task Handle(PdfDeliveredToClient message, IMessageHandlerContext context)
    {
        Logger.LogInformation("Received PdfDeliveredToClient with EventId={EventId}, for EvaluationId={EvaluationId}",
            message.EventId, message.EvaluationId);

        using var transaction = TransactionSupplier.BeginTransaction();
        if (await HasPdfDeliveredToClient(message.EvaluationId, context.CancellationToken))
        {
            Logger.LogInformation(
                "Already processed a PdfDeliveredToClient event for EvaluationId={EvaluationId}, ignoring EventId={EventId}",
                message.EvaluationId, message.EventId);
            return;
        }

        var pdfEntity = await Mediator.Send(new AddPdfDeliveredToClient(message), context.CancellationToken);
        var exam = await Mediator.Send(new QueryExam(message.EvaluationId), context.CancellationToken)
                   ?? throw new ExamNotFoundByEvaluationException(message.EvaluationId, message.EventId);

        await context.SendLocal(new BillableExamStatusEvent
        {
            EventId = message.EventId,
            ExamId = exam.ExamId,
            StatusCode = ExamStatusCode.ClientPdfDelivered,
            EvaluationId = exam.EvaluationId,
            StatusDateTime = message.CreatedDateTime,
            PdfDeliveryDateTime = pdfEntity.DeliveryDateTime
        });
        
        //check if the exam was performed before billing
        var notPerformed = await Mediator.Send(new QueryExamNotPerformed(message.EvaluationId), context.CancellationToken);
        if (notPerformed != null)
        {
            Logger.LogInformation("eGFR PDF was delivered for EvaluationId={EvaluationId} but exam was not performed, skipping billing", message.EvaluationId);

            PublishObservabilityEvents(message.EvaluationId, message.CreatedDateTime,
                Observability.PdfDelivered.PdfDeliveryReceivedEventButExamNotPerformed,
                null, true);
            await transaction.CommitAsync(context.CancellationToken);
            return;
        }

        if (featureFlags.EnableDirectBilling)
        {
            await context.SendLocal(new ProcessBillingEvent(message.EventId, pdfEntity.EvaluationId,
                pdfEntity.PdfDeliveredToClientId, true, message.CreatedDateTime, ProductCodes.EGfrRcmBillingLeft));
        }

        string normalityCode;
        try
        {
            normalityCode = await GetNormalityCode(exam, context.CancellationToken);
        }
        catch (LabResultNotFoundException)
        {
            Logger.LogInformation(
                        "Awaiting LabResult for Exam with EvaluationId={EvaluationId}, EventId={EventId}",
                        message.EvaluationId, message.EventId);
                    await transaction.CommitAsync(context.CancellationToken);
                    return;
        }

        var billable = billableRules.IsBillable(new BillableRuleAnswers(message.EvaluationId, message.EventId) { NormalityCode = normalityCode });

        if (featureFlags.EnableDirectBilling)
        {
            await context.SendLocal(new ProcessBillingEvent(message.EventId, pdfEntity.EvaluationId,
                pdfEntity.PdfDeliveredToClientId, billable.IsMet, message.CreatedDateTime, ProductCodes.EGfrRcmBillingResults));
        }
        else
        {
            await context.SendLocal(new ProcessBillingEvent(message.EventId, pdfEntity.EvaluationId,
                pdfEntity.PdfDeliveredToClientId, billable.IsMet, message.CreatedDateTime, ProductCodes.eGFR_RcmBilling));
        }

        await transaction.CommitAsync(context.CancellationToken);

        PublishObservabilityEvents(message.EvaluationId, message.CreatedDateTime,
            Observability.PdfDelivered.PdfDeliveryReceivedEvent, null, true);
    }

    private async Task<string> GetNormalityCode(Exam exam, CancellationToken cancellationToken)
    {
        // Check LabResult table for LabResult if not found check QuestLabResult table
        var labResult = await GetLabResult(exam.ExamId, cancellationToken);
        if (labResult is not null)
        {
            return mapper.Map<ResultsReceived>(labResult).Determination;
        }

        //Check if quest LabResult exist
        var labQuestResult = await GetQuestLabResult(exam.EvaluationId, cancellationToken);
        if (labQuestResult is not null)
        {
            return labQuestResult.NormalityCode;
        }

        throw new LabResultNotFoundException(exam.EvaluationId);
    }

    private async Task<bool> HasPdfDeliveredToClient(long evaluationId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new QueryPdfDeliveredToClient(evaluationId), cancellationToken);

        return result.Entity != null;
    }

    /// <summary>
    /// Check if QuestLabResults were received by looking at the database for ResultDataDownloaded status
    /// </summary>
    /// <param name="evaluationId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<QuestLabResult> GetQuestLabResult(long evaluationId, CancellationToken cancellationToken)
        => await Mediator.Send(new QueryQuestLabResultByEvaluationId(evaluationId), cancellationToken);

    /// <summary>
    /// Check if LabResults were received by looking at the database for ResultDataDownloaded status
    /// </summary>
    /// <param name="examId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<LabResult> GetLabResult(int examId, CancellationToken cancellationToken)
        => await Mediator.Send(new QueryLabResultByExamId(examId), cancellationToken);
}