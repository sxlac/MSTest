using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using PdfDeliveredToClient = UacrEvents.PdfDeliveredToClient;
using Signify.uACR.Core.BusinessRules;
using Signify.uACR.Core.Commands;
using Signify.uACR.Core.Constants;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Data;
using Signify.uACR.Core.Exceptions;
using Signify.uACR.Core.FeatureFlagging;
using Signify.uACR.Core.Models;
using Signify.uACR.Core.Queries;
using System.Threading.Tasks;
using System.Threading;
using Signify.Dps.Observability.Library.Services;
using Signify.uACR.Core.Infrastructure;
using UacrNsbEvents;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public class PdfDeliveredToClientHandler(
    ILogger<PdfDeliveredToClientHandler> logger,
    ITransactionSupplier transactionSupplier,
    IMediator mediator,
    IBillableRules billableRules,
    IPublishObservability publishObservability,
    IFeatureFlags featureFlags,
    IApplicationTime applicationTime)
    : BaseNsbHandler(logger, mediator, transactionSupplier, publishObservability, applicationTime), IHandleMessages<PdfDeliveredToClient>
{
    [Transaction]
    public async Task Handle(PdfDeliveredToClient message, IMessageHandlerContext context)
    {
        Logger.LogInformation("Received PdfDeliveredToClient with EventId={EventId}, for EvaluationId={EvaluationId}",
            message.EventId, message.EvaluationId);

        using var transaction = TransactionSupplier.BeginTransaction();
        if (await HasPdfDeliveryEvent(message.EvaluationId, context.CancellationToken))
        {
            Logger.LogInformation(
                "Already processed a PdfDeliveredToClient event for EvaluationId={EvaluationId}, ignoring EventId={EventId}",
                message.EvaluationId, message.EventId);
            return;
        }

        var pdfEntity = await Mediator.Send(new AddPdfDeliveredToClient(message), context.CancellationToken);
        
        var exam = await Mediator.Send(new QueryExamByEvaluation { EvaluationId = message.EvaluationId }, context.CancellationToken)
                   ?? throw new ExamNotFoundException(message.EvaluationId, message.EventId);

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
            Logger.LogInformation("uACR PDF was delivered for EvaluationId={EvaluationId} but exam was not performed, skipping billing.", message.EvaluationId);

            PublishObservabilityEvents(message.EvaluationId, message.CreatedDateTime, 
                Observability.PdfDelivered.PdfDeliveryReceivedEventButExamNotPerformed, null, true);
            await transaction.CommitAsync(context.CancellationToken);
            return;
        }

        if (featureFlags.EnableDirectBilling)
        {
            await context.SendLocal(new ProcessBillingEvent(message.EventId, pdfEntity.EvaluationId,
                pdfEntity.PdfDeliveredToClientId, true, message.CreatedDateTime, ProductCodes.UAcrRcmBillingLeft));
        }
        
        if (featureFlags.EnableBilling) {
            var labResult = await GetLabResult(exam.EvaluationId,context.CancellationToken);
                if (labResult is null)
                {
                    Logger.LogInformation("Awaiting Result for Exam with EvaluationId={EvaluationId}, EventId={EventId}",
                        message.EvaluationId, message.EventId);
                    await transaction.CommitAsync(context.CancellationToken);
                    return;
                }

                var billable = billableRules.IsBillable(new BillableRuleAnswers(message.EvaluationId, message.EventId)
                    { Result = labResult });

                if (featureFlags.EnableDirectBilling)
                {
                    await context.SendLocal(new ProcessBillingEvent(message.EventId, pdfEntity.EvaluationId,
                        pdfEntity.PdfDeliveredToClientId, billable.IsMet, message.CreatedDateTime, ProductCodes.UAcrRcmBillingResults));
                }
                else
                {
                    await context.SendLocal(new ProcessBillingEvent(message.EventId, pdfEntity.EvaluationId,
                        pdfEntity.PdfDeliveredToClientId, billable.IsMet, message.CreatedDateTime, ProductCodes.uACR_RcmBilling));
                }
        }
        
        await transaction.CommitAsync(context.CancellationToken);

        PublishObservabilityEvents(message.EvaluationId, message.CreatedDateTime,
            Observability.PdfDelivered.PdfDeliveryReceivedEvent, null, true);
    }

    private async Task<bool> HasPdfDeliveryEvent(long evaluationId, CancellationToken token)
    {
        var result = await Mediator.Send(new QueryPdfDeliveredToClient(evaluationId), token);

        return result.Entity != null;
    }
    
    /// <summary>
    /// Check if LabResults were received 
    /// </summary>
    /// <param name="evaluationId"></param>
    /// <returns></returns>
    private async Task<LabResult> GetLabResult(long evaluationId, CancellationToken token) => await Mediator.Send(new QueryLabResultByEvaluationId(evaluationId), token);
}