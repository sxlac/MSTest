using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.Spirometry.Core.Events;
using Signify.Spirometry.Core.FeatureFlagging;
using Signify.Spirometry.Core.Filters;

namespace Signify.Spirometry.Core.EventHandlers.Akka;

public class BillRequestAcceptedHandler : IHandleEvent<BillRequestAccepted>
{
    private readonly ILogger _logger;
    private readonly IMessageSession _session;
    private readonly IProductFilter _productFilter;
    private readonly IFeatureFlags _featureFlags;

    public BillRequestAcceptedHandler(ILogger<BillRequestAcceptedHandler> logger,
        IMessageSession session,
        IProductFilter productFilter,
        IFeatureFlags featureFlags)
    {
        _logger = logger;
        _session = session;
        _featureFlags = featureFlags;
        _productFilter = productFilter;
    }

    [Transaction]
    public async Task Handle(BillRequestAccepted @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received BillRequestAcceptedEvent with RCMBillId {RcmBillId} ", @event.RCMBillId);

        if (_featureFlags.EnableBillAccepted)
        {
            if (!_productFilter.ShouldProcess(@event.RCMProductCode))
            {
                _logger.LogInformation("Product code not found, bill accepted event ignored, for RCMBillId={RcmBillId}", @event.RCMBillId);
                return;
            }

            await _session.SendLocal(@event, cancellationToken);
        }
    }
}