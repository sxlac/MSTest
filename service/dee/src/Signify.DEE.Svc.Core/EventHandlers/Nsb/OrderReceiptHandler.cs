using Iris.Public.Types.Models.Public._2._3._1;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.DEE.Svc.Core.Constants;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Infrastructure;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Queries;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.EventHandlers.Nsb;

public class OrderReceiptHandler(
    ILogger<OrderReceiptHandler> logger,
    IMediator mediator,
    ITransactionSupplier transactionSupplier,
    IApplicationTime applicationTime)
    : IHandleMessages<OrderReceipt>
{
    [Transaction]
    public async Task Handle(OrderReceipt message, IMessageHandlerContext context)
    {
        logger.LogInformation("Received OrderReceipt for Order Local ID: {OrderLocalId}", message.OrderLocalId);

        if (!message.Success)
        {
            logger.LogWarning("OrderReceipt failed for exam local ID: {OrderLocalId} and Iris order Id {IrisOrderId} because {ErrorMessage}", message.OrderLocalId, message.IrisOrderId, message.ErrorMessage);
            return;
        }

        var orderLocalId = message.OrderLocalId;
        var exam = await mediator.Send(new GetExamByLocalId() { LocalId = orderLocalId }, context.CancellationToken);
        if (exam is null)
        {
            // If a transaction is aborted, Iris will still receive the order and may confirm receipt of it
            // but we never kept that exam in the DB.
            logger.LogInformation("OrderReceipt could not find exam by local ID: {OrderLocalId}", message.OrderLocalId);
            return;
        }

        var transaction = transactionSupplier.BeginTransaction();

        await mediator.Send(new CreateStatus(exam.ExamId, ExamStatusCode.IRISExamCreated.Name, applicationTime.UtcNow()), context.CancellationToken);

        logger.LogInformation("Finished recording OrderReceipt for Order Local ID: {OrderLocalId}", message.OrderLocalId);

        await transaction.CommitAsync(context.CancellationToken);
        await mediator.Send(new RegisterObservabilityEvent { EvaluationId = (long)exam.EvaluationId, EventType = Observability.DeeStatusEvents.IrisOrderReceivedEvent }, context.CancellationToken);
    }
}