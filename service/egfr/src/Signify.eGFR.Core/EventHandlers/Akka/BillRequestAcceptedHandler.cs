using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.eGFR.Core.Events;
using Signify.eGFR.Core.FeatureFlagging;
using Signify.eGFR.Core.Filters;

namespace Signify.eGFR.Core.EventHandlers.Akka;

public class BillRequestAcceptedHandler(
    ILogger<BillRequestAcceptedHandler> logger,
    IMessageSession session,
    IProductFilter productFilter,
    IFeatureFlags featureFlags)
    : IHandleEvent<BillRequestAccepted>
{
    private readonly ILogger _logger = logger;

    [Transaction]
    public async Task Handle(BillRequestAccepted @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received BillRequestAcceptedEvent with RCMBillId {RcmBillId} ", @event.RCMBillId);

        if (featureFlags.EnableBillAccepted)
        {
            if (!productFilter.ShouldProcess(@event.RCMProductCode))
            {
                _logger.LogInformation("Product code not found, bill accepted event ignored, for RCMBillId={RcmBillId}", @event.RCMBillId);
                return;
            }

            await session.SendLocal(@event, cancellationToken);
        }
    }
}