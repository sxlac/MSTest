using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.HBA1CPOC.Svc.Core.Constants;
using Signify.HBA1CPOC.Svc.Core.Events;

namespace Signify.HBA1CPOC.Svc.Core.EventHandlers.Akka;

public class BillRequestAcceptedHandler : IHandleEvent<BillRequestAccepted>
{
    private readonly ILogger _logger;
    private readonly IMessageSession _session;
    public BillRequestAcceptedHandler(ILogger<BillRequestAcceptedHandler> logger,
        IMessageSession session)
    {
        _logger = logger;
        _session = session;
    }

    [Transaction]
    public async Task Handle(BillRequestAccepted @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received BillRequestAcceptedEvent with RCMBillId {RCMBillId} ", @event.RCMBillId);

        var shouldProcess =
            @event.RCMProductCode.Equals(ApplicationConstants.ProductCode, StringComparison.OrdinalIgnoreCase);

        if (!shouldProcess)
        {
            _logger.LogInformation(
                "Product code not found, bill accepted event ignored, for RCMBillId={RCMBillId}",
                @event.RCMBillId);
            return;
        }

        await _session.SendLocal(@event);
    }
}