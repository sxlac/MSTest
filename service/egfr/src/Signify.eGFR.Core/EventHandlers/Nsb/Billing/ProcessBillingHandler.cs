using EgfrNsbEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Queries;
using System.Threading.Tasks;
using System.Threading;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

/// <summary>
/// NSB Handler to process a ProcessPdfDeliveredToClientEvent. Handles either raising a BillableEvent, or
/// tracking a BillingRequestNotSent status.
/// </summary>
public class ProcessBillingHandler(
    ILogger<ProcessBillingHandler> logger,
    ITransactionSupplier transactionSupplier,
    IMediator mediator)
    : IHandleMessages<ProcessBillingEvent>
{
    [Transaction]
    public async Task Handle(ProcessBillingEvent message, IMessageHandlerContext context)
    {
        logger.LogInformation("Processing billing for EvaluationId={EvaluationId}, PdfDeliveredToClientId={PdfDeliveredToClientId}, IsBillable={IsBillable}",
            message.EvaluationId, message.PdfDeliveredToClientId, message.IsBillable);

        var exam = await mediator.Send(new QueryExam(message.EvaluationId), context.CancellationToken);
        var pdfEntity = (await mediator.Send(new QueryPdfDeliveredToClient(message.EvaluationId), context.CancellationToken)).Entity;

        using var transaction = transactionSupplier.BeginTransaction();
        
        if (!message.IsBillable)
        {
            logger.LogInformation("Not sending a billing request for EventId={EventId}, because this EvaluationId={EvaluationId} is not billable", pdfEntity.EventId, message.EvaluationId);

            await context.SendLocal(new BillableExamStatusEvent
            {
                EventId = message.EventId,
                ExamId = exam.ExamId,
                StatusCode = ExamStatusCode.BillRequestNotSent,
                EvaluationId = exam.EvaluationId,
                StatusDateTime = message.StatusDateTime,
                PdfDeliveryDateTime = pdfEntity.DeliveryDateTime,
                RcmProductCode = message.RcmProductCode
            });
            
            await Complete(transaction, context.CancellationToken);
            return;
        }

        await context.SendLocal(new ExamStatusEvent
        {
            EventId = message.EventId,
            EvaluationId = message.EvaluationId,
            StatusCode = ExamStatusCode.BillableEventReceived,
            StatusDateTime = pdfEntity.DeliveryDateTime,
            RcmProductCode = message.RcmProductCode
        });
        
        var billRequestSent = (await mediator.Send(new QueryBillRequestSent(message.EvaluationId, message.RcmProductCode), context.CancellationToken)).Entity;
        if (billRequestSent != null)
        {
            // No matter if one or more PDFs are delivered to the client for an evaluation, we can only bill for a performed eGFR exam once
            logger.LogInformation("Already sent a billing request for EvaluationId={EvaluationId}, nothing left to do for EventId={EventId}", message.EvaluationId, pdfEntity.EventId);
            await Complete(transaction, context.CancellationToken);
            return;
        }

        await context.SendLocal(new CreateBillEvent
        {
            EvaluationId = exam.EvaluationId,
            PdfDeliveryDateTime = pdfEntity.DeliveryDateTime,
            BillableDate = pdfEntity.DeliveryDateTime,
            EventId = message.EventId,
            RcmProductCode = message.RcmProductCode
        });

        await Complete(transaction, context.CancellationToken);
        return;

        async Task Complete(IBufferedTransaction tran, CancellationToken token)
        {
            await tran.CommitAsync(token);
        }
    }
}