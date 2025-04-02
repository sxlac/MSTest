using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.uACR.Core.Events.Akka;
using Signify.uACR.Core.FeatureFlagging;
using Signify.uACR.Core.Filters;

namespace Signify.uACR.Core.EventHandlers.Akka;

public class BillRequestAcceptedHandler(
    ILogger<BillRequestAcceptedHandler> logger,
    IMessageSession session,
    IProductFilter productFilter,
    IFeatureFlags featureFlags)
    : IHandleEvent<BillRequestAccepted>
{
    [Transaction]
    public async Task Handle(BillRequestAccepted @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received BillRequestAcceptedEvent with RCMBillId {RcmBillId} ", @event.RCMBillId);

        if (featureFlags.EnableBillAccepted)
        {
            if (!productFilter.ShouldProcess(@event.RCMProductCode))
            {
                logger.LogInformation("Product code not found, bill accepted event ignored,for RCMBillId={RcmBillId}", @event.RCMBillId);
                return;
            }

            await session.SendLocal(@event, cancellationToken: cancellationToken);
        }
    }
}