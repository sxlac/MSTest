using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Constants;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Exceptions;
using Signify.FOBT.Svc.Core.Infrastructure;
using Signify.FOBT.Svc.Core.Infrastructure.Observability;
using Signify.FOBT.Svc.Core.Queries;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public class RcmBillRequestAcceptedHandler : IHandleMessages<BillRequestAccepted>
{
    private readonly ILogger _logger;
    private readonly IApplicationTime _applicationTime;
    private readonly ITransactionSupplier _transactionSupplier;
    private readonly IMediator _mediator;
    private readonly IPublishObservability _publishObservability;

    public RcmBillRequestAcceptedHandler(ILogger<RcmBillRequestAcceptedHandler> logger,
        IApplicationTime applicationTime,
        ITransactionSupplier transactionSupplier,
        IMediator mediator,
        IPublishObservability publishObservability)
    {
        _logger = logger;
        _applicationTime = applicationTime;
        _transactionSupplier = transactionSupplier;
        _mediator = mediator;
        _publishObservability = publishObservability;
    }

    [Transaction]
    public async Task Handle(BillRequestAccepted message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Received BillRequestAccepted event with RCMBillIId {RcmBillId}", message.RCMBillId);

        using var transaction = _transactionSupplier.BeginTransaction();

        var rcmBillingRecord = await _mediator.Send(new GetFobtBillingByBillId(message.RCMBillId.ToString()),
            context.CancellationToken);

        if (rcmBillingRecord is null)
        {
            _logger.LogInformation(
                "RCM billing record does not exist for RCMBillId {RcmBillId}. Checking if event contains an evaluationID",
                message.RCMBillId);
            if (message.AdditionalDetails is not null &&
                message.AdditionalDetails.ContainsKey(Observability.EventParams.EvaluationId))
            {
                PublishObservability(message, Observability.RcmBilling.BillAcceptedNotFoundEvent);
                throw new BillNotFoundException(TryGetEvaluationId(message), message.RCMBillId);
            }

            PublishObservability(message, Observability.RcmBilling.BillAcceptedNotTrackedEvent);
        }
        else
        {
            rcmBillingRecord.Accepted = true;
            rcmBillingRecord.AcceptedAt = _applicationTime.UtcNow();
            await _mediator.Send(new CreateOrUpdateRcmBilling { RcmBilling = rcmBillingRecord },
                context.CancellationToken);

            PublishObservability(message, Observability.RcmBilling.BillAcceptedSuccessEvent);
        }

        await transaction.CommitAsync(context.CancellationToken);
    }

    [Trace]
    private void PublishObservability(BillRequestAccepted message, string eventType)
    {
        var evaluationId = TryGetEvaluationId(message);
        var observabilityPdfDeliveryReceivedEvent = new ObservabilityEvent
        {
            EvaluationId = evaluationId,
            EventType = eventType,
            EventValue = new Dictionary<string, object>
            {
                { Observability.EventParams.EvaluationId, evaluationId },
                {
                    Observability.EventParams.CreatedDateTime,
                    ((DateTimeOffset)_applicationTime.UtcNow()).ToUnixTimeSeconds()
                },
                { Observability.EventParams.BillId, message.RCMBillId.ToString() }
            }
        };

        _publishObservability.RegisterEvent(observabilityPdfDeliveryReceivedEvent, true);
    }

    [Trace]
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