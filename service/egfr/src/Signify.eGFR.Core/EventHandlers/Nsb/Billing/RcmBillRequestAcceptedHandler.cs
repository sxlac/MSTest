using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Constants;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Events;
using Signify.eGFR.Core.Exceptions;
using Signify.eGFR.Core.Infrastructure;
using Signify.eGFR.Core.Queries;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Signify.Dps.Observability.Library.Services;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public class RcmBillRequestAcceptedHandler(
    ILogger<RcmBillRequestAcceptedHandler> logger,
    IMediator mediator,
    ITransactionSupplier transactionSupplier,
    IPublishObservability publishObservability,
    IApplicationTime applicationTime)
    : BaseNsbHandler(logger, mediator, transactionSupplier, publishObservability, applicationTime), IHandleMessages<BillRequestAccepted>
{
    [Transaction]
    public async Task Handle(BillRequestAccepted message, IMessageHandlerContext context)
    {
        Logger.LogInformation("Received BillRequestAccepted event with RCMBillId={RcmBillId}", message.RCMBillId);

        using var transaction = TransactionSupplier.BeginTransaction();

        var billRequestSentResult = await Mediator.Send(new QueryBillRequestSent(message.RCMBillId), context.CancellationToken);

        if (billRequestSentResult?.Entity is null)
        {
            Logger.LogInformation("RCM billing record does not exist for RCMBillId={RcmBillId}. Checking if event contains an evaluationID",
                message.RCMBillId);
            if (message.AdditionalDetails is not null && message.AdditionalDetails.ContainsKey(Observability.EventParams.EvaluationId))
            {
                PublishObservabilityEvents(TryGetEvaluationId(message), ApplicationTime.UtcNow(),
                    Observability.RcmBilling.BillAcceptedNotFoundEvent,
                    new Dictionary<string, object>
                    {
                        { Observability.EventParams.BillId, message.RCMBillId.ToString() }
                    }, true);
                throw new BillNotFoundException(TryGetEvaluationId(message), message.RCMBillId);
            }

            PublishObservabilityEvents(TryGetEvaluationId(message), ApplicationTime.UtcNow(),
                Observability.RcmBilling.BillAcceptedNotTrackedEvent,
                new Dictionary<string, object>
                {
                    { Observability.EventParams.BillId, message.RCMBillId.ToString() }
                }, true);
        }
        else
        {
            billRequestSentResult.Entity.Accepted = true;
            billRequestSentResult.Entity.AcceptedAt = ApplicationTime.UtcNow();
            await Mediator.Send(new AddOrUpdateBillRequestSent(billRequestSentResult.Entity), context.CancellationToken);

            PublishObservabilityEvents(TryGetEvaluationId(message), ApplicationTime.UtcNow(),
                Observability.RcmBilling.BillAcceptedSuccessEvent,
                new Dictionary<string, object>
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