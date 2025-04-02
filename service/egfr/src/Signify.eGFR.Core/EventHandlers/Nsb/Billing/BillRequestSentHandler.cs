using EgfrNsbEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Infrastructure;
using Signify.eGFR.Core.Queries;
using System.Threading.Tasks;
using System.Threading;
using Signify.Dps.Observability.Library.Services;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

/// <summary>
/// NSB event handler for the <see cref="BillRequestSentEvent"/>
/// </summary>
public class BillRequestSentHandler(
    ILogger<BillRequestSentHandler> logger,
    IMediator mediator,
    ITransactionSupplier transactionSupplier,
    IPublishObservability publishObservability,
    IApplicationTime applicationTime)
    : BaseNsbHandler(logger, mediator, transactionSupplier, publishObservability, applicationTime), IHandleMessages<BillRequestSentEvent>
{
    [Transaction]
    public async Task Handle(BillRequestSentEvent message, IMessageHandlerContext context)
    {
        if (await HasBillRequestSent(message.EvaluationId, message.RcmProductCode, context.CancellationToken))
        {
            Logger.LogInformation(
                "Already processed a BillRequestSent event for EvaluationId={EvaluationId}, ignoring EventId={EventId}",
                message.EvaluationId, message.EventId);
            return;
        }

        Logger.LogInformation("Received BillRequestSent with EventId={EventId}, for EvaluationId={EvaluationId}",
            message.EventId, message.EvaluationId);

        using var transaction = TransactionSupplier.BeginTransaction();
        
        var addCommand = new AddOrUpdateBillRequestSent(message.EventId, message.EvaluationId, new BillRequestSent
        {
            BillId = message.BillId,
            ExamId = message.ExamId,
            CreatedDateTime = ApplicationTime.UtcNow(),
            BillingProductCode = message.RcmProductCode
        });

        await Mediator.Send(addCommand, context.CancellationToken);
        
        await transaction.CommitAsync(context.CancellationToken);
    }

    private async Task<bool> HasBillRequestSent(long evaluationId, string billingProductCode, CancellationToken token)
    {
        var result = await Mediator.Send(new QueryBillRequestSent(evaluationId, billingProductCode), token);

        return result.Entity != null;
    }
}
