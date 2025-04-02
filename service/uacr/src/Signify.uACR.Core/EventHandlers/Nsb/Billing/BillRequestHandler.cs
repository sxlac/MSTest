using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.uACR.Core.Commands;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Data;
using Signify.uACR.Core.Infrastructure;
using Signify.uACR.Core.Queries;
using System.Threading.Tasks;
using System.Threading;
using Signify.Dps.Observability.Library.Services;
using UacrNsbEvents;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

/// <summary>
/// NSB event handler for the <see cref="BillRequestEvent"/>
/// </summary>
public class BillRequestHandler(
    ILogger<BillRequestHandler> logger,
    IMediator mediator,
    ITransactionSupplier transactionSupplier,
    IPublishObservability publishObservability,
    IApplicationTime applicationTime)
    : BaseNsbHandler(logger, mediator, transactionSupplier, publishObservability, applicationTime), IHandleMessages<BillRequestEvent>
{
    [Transaction]
    public async Task Handle(BillRequestEvent message, IMessageHandlerContext context)
    {
        if (await HasBillRequest(message.EvaluationId, message.RcmProductCode, context.CancellationToken))
        {
            Logger.LogInformation(
                "Already processed a BillRequest event for EvaluationId={EvaluationId}, ignoring EventId={EventId}",
                message.EvaluationId, message.EventId);
            return;
        }

        Logger.LogInformation("Received BillRequest with EventId={EventId}, for EvaluationId={EvaluationId}",
            message.EventId, message.EvaluationId);

        using var transaction = TransactionSupplier.BeginTransaction();
        
        var addCommand = new AddOrUpdateBillRequest(message.EventId, message.EvaluationId, new BillRequest
        {
            BillId = message.BillId,
            ExamId = message.ExamId,
            CreatedDateTime = ApplicationTime.UtcNow(),
            BillingProductCode = message.RcmProductCode
        });

        await Mediator.Send(addCommand, context.CancellationToken);
        
        await transaction.CommitAsync(context.CancellationToken);
    }

    private async Task<bool> HasBillRequest(long evaluationId, string billingProductCode, CancellationToken token)
    {
        var result = await Mediator.Send(new QueryBillRequests(evaluationId, billingProductCode), token);

        return result.Entity != null;
    }
}
