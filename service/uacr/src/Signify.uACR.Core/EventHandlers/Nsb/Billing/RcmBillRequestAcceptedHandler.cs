using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.Dps.Observability.Library.Services;
using Signify.uACR.Core.Commands;
using Signify.uACR.Core.Constants;
using Signify.uACR.Core.Data;
using Signify.uACR.Core.Events.Akka;
using Signify.uACR.Core.Exceptions;
using Signify.uACR.Core.Infrastructure;
using Signify.uACR.Core.Queries;
using System.Collections.Generic;
using System.Threading.Tasks;
using NsbEventHandlers;

namespace Signify.uACR.Core.EventHandlers.Nsb;

public class RcmBillRequestAcceptedHandler(
    ILogger<RcmBillRequestAcceptedHandler> logger,
    IApplicationTime applicationTime,
    ITransactionSupplier transactionSupplier,
    IMediator mediator,
    IPublishObservability publishObservability)
    : BaseNsbHandler(logger, mediator, transactionSupplier, publishObservability, applicationTime), IHandleMessages<BillRequestAccepted>
{
    [Transaction]
    public async Task Handle(BillRequestAccepted message, IMessageHandlerContext context)
    {
        Logger.LogInformation("Received BillRequestAccepted event with RCMBillId={RcmBillId}", message.RCMBillId);

        using var transaction = TransactionSupplier.BeginTransaction();
        
        var billRequestSentResult = await Mediator.Send(new QueryBillRequests(message.RCMBillId), context.CancellationToken);

        if (billRequestSentResult?.Entity is null)
        {
            Logger.LogInformation("RCM billing record does not exist for RCMBillId={RcmBillId}. Checking if event contains an evaluationID",
                message.RCMBillId);
            if (message.AdditionalDetails is not null &&
                message.AdditionalDetails.ContainsKey(Observability.EventParams.EvaluationId))
            {
                PublishObservabilityEvents(TryGetEvaluationId(message), ApplicationTime.UtcNow(),
                    Observability.RcmBilling.BillAcceptedNotFoundEvent, 
                    new Dictionary<string, object>()
                    {
                        { Observability.EventParams.BillId, message.RCMBillId.ToString() }
                    }, true);
                throw new BillNotFoundException(TryGetEvaluationId(message), message.RCMBillId);
            }

            PublishObservabilityEvents(TryGetEvaluationId(message), ApplicationTime.UtcNow(),
                Observability.RcmBilling.BillAcceptedNotTrackedEvent,
                new Dictionary<string, object>()
                {
                    { Observability.EventParams.BillId, message.RCMBillId.ToString() }
                }, true);
        }
        else
        {
            billRequestSentResult.Entity.Accepted = true;
            billRequestSentResult.Entity.AcceptedAt = ApplicationTime.UtcNow();
            await Mediator.Send(new AddOrUpdateBillRequest(billRequestSentResult.Entity), context.CancellationToken);

            PublishObservabilityEvents(TryGetEvaluationId(message), ApplicationTime.UtcNow(),
                Observability.RcmBilling.BillAcceptedSuccessEvent,
                new Dictionary<string, object>()
                {
                    { Observability.EventParams.BillId, message.RCMBillId.ToString() }
                }, true);
        }

        await transaction.CommitAsync(context.CancellationToken);
    }

    private static long TryGetEvaluationId(BillRequestAccepted message)
    {
        if (message.AdditionalDetails is not null &&
            message.AdditionalDetails.TryGetValue(Observability.EventParams.EvaluationId, out var evaluationId))
        {
            return long.TryParse(evaluationId, out var id) ? id : 0;
        }

        return 0;
    }
}