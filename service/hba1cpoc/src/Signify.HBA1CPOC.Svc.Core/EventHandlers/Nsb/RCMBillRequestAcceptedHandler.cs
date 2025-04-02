using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Constants;
using Signify.HBA1CPOC.Svc.Core.Data;
using Signify.HBA1CPOC.Svc.Core.Events;
using Signify.HBA1CPOC.Svc.Core.Exceptions;
using Signify.HBA1CPOC.Svc.Core.Infrastructure;
using Signify.HBA1CPOC.Svc.Core.Infrastructure.Observability;
using Signify.HBA1CPOC.Svc.Core.Queries;

namespace Signify.HBA1CPOC.Svc.Core.EventHandlers.Nsb;

public class RcmBillRequestAcceptedHandler(
    ILogger<RcmBillRequestAcceptedHandler> logger,
    IApplicationTime applicationTime,
    ITransactionSupplier transactionSupplier,
    IMediator mediator,
    IPublishObservability publishObservability)
    : IHandleMessages<BillRequestAccepted>
{
    private readonly ILogger _logger = logger;

    [Transaction]
    public async Task Handle(BillRequestAccepted message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Received BillRequestAccepted event with RCMBillId {RcmBillId}", message.RCMBillId);
        
        using var transaction = transactionSupplier.BeginTransaction();

        var rcmBillingRecord = await mediator.Send(new GetRcmBillingByBillId
        {
            BillId = message.RCMBillId.ToString()
        }, context.CancellationToken);
        
        if (rcmBillingRecord is null)
        {
            _logger.LogInformation("RCM billing record does not exist for RCMBillId {RcmBillId}. Checking if event contains an evaluationID", message.RCMBillId);
            if (message.AdditionalDetails is not null && message.AdditionalDetails.ContainsKey(ApplicationConstants.EvaluationId))
            {
                PublishObservability(message, Observability.RcmBilling.BillAcceptedNotFoundEvent);
                throw new BillIdNotFoundException(TryGetEvaluationId(message), message.RCMBillId);
            }
            PublishObservability(message, Observability.RcmBilling.BillAcceptedNotTrackedEvent);
        }
        else
        {
            rcmBillingRecord.Accepted = true;
            rcmBillingRecord.AcceptedAt = applicationTime.UtcNow();
            await mediator.Send(new CreateOrUpdateRCMBilling
            {
                RcmBilling = rcmBillingRecord
            }, context.CancellationToken);
            
            PublishObservability(message, Observability.RcmBilling.BillAcceptedSuccessEvent);
        }
        await transaction.CommitAsync(context.CancellationToken);
    }
    
    private void PublishObservability(BillRequestAccepted message, string eventType)
    {
        var observabilityPdfDeliveryReceivedEvent = new ObservabilityEvent
        {
            EvaluationId = TryGetEvaluationId(message),
            EventType = eventType,
            EventValue = new Dictionary<string, object>
            {
                {Observability.EventParams.EvaluationId, TryGetEvaluationId(message) },
                {Observability.EventParams.CreatedDateTime, ((DateTimeOffset)applicationTime.UtcNow()).ToUnixTimeSeconds() },
                {Observability.EventParams.BillId, message.RCMBillId.ToString()}
            }
        };

        publishObservability.RegisterEvent(observabilityPdfDeliveryReceivedEvent, true);
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