using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.DEE.Svc.Core.Constants;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Events;
using Signify.DEE.Svc.Core.Exceptions;
using Signify.DEE.Svc.Core.Infrastructure;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Queries;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;

namespace Signify.DEE.Svc.Core.EventHandlers.Nsb;

public class RcmBillRequestAcceptedHandler(
    ILogger<RcmBillRequestAcceptedHandler> logger,
    IApplicationTime applicationTime,
    ITransactionSupplier transactionSupplier,
    IMediator mediator,
    IPublishObservability publishObservability)
    : IHandleMessages<BillRequestAccepted>
{
    [Transaction]
    public async Task Handle(BillRequestAccepted message, IMessageHandlerContext context)
    {
        logger.LogInformation("Received BillRequestAccepted event with RCMBillId={RcmBillId}", message.RCMBillId);

        using var transaction = transactionSupplier.BeginTransaction();
        
        var rcmBillingRecord = await mediator.Send(new GetRcmBillingByBillId(message.RCMBillId.ToString()), context.CancellationToken);

        if (rcmBillingRecord is null)
        {
            logger.LogInformation("RCM billing record does not exist for RCMBillId={RcmBillId}. Checking if event contains an evaluationID",
                message.RCMBillId);
            if (message.AdditionalDetails is not null && message.AdditionalDetails.ContainsKey(ApplicationConstants.EvaluationId))
            {
                PublishObservability(message, Observability.RcmBilling.BillAcceptedNotFoundEvent, true);
                throw new BillNotFoundException(TryGetEvaluationId(message), message.RCMBillId);
            }

            PublishObservability(message, Observability.RcmBilling.BillAcceptedNotTrackedEvent, true);
        }
        else
        {
            rcmBillingRecord.Accepted = true;
            rcmBillingRecord.AcceptedAt = applicationTime.UtcNow();
            await mediator.Send(new CreateRcmBilling { RcmBilling = rcmBillingRecord }, context.CancellationToken);
            PublishObservability(message, Observability.RcmBilling.BillAcceptedSuccessEvent, true);
        }

        await transaction.CommitAsync(context.CancellationToken);
    }

    private void PublishObservability(BillRequestAccepted message, string eventType, bool sendImmediately)
    {
        var observabilityPdfDeliveryReceivedEvent = new ObservabilityEvent
        {
            EvaluationId = TryGetEvaluationId(message),
            EventType = eventType,
            EventValue = new Dictionary<string, object>
            {
                { Observability.EventParams.EvaluationId, TryGetEvaluationId(message) },
                { Observability.EventParams.CreatedDateTime, ((DateTimeOffset)applicationTime.UtcNow()).ToUnixTimeSeconds() },
                { Observability.EventParams.BillId, message.RCMBillId.ToString() }
            }
        };

        publishObservability.RegisterEvent(observabilityPdfDeliveryReceivedEvent, sendImmediately);
    }

    private static long TryGetEvaluationId(BillRequestAccepted message)
    {
        if (message.AdditionalDetails is not null &&
            message.AdditionalDetails.TryGetValue(ApplicationConstants.EvaluationId, out var evaluationId))
        {
            return long.TryParse(evaluationId, out var id) ? id : 0;
        }
        return 0;
    }
}