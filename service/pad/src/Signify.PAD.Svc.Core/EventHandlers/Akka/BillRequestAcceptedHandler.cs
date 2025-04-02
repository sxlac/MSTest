using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.Core.Filters;

namespace Signify.PAD.Svc.Core.EventHandlers.Akka;

public class BillRequestAcceptedHandler : IHandleEvent<BillRequestAccepted>
{
    private readonly ILogger _logger;
    private readonly IMessageSession _session;
    private readonly IProductFilter _productFilter;

    public BillRequestAcceptedHandler(ILogger<BillRequestAcceptedHandler> logger,
        IMessageSession session,
        IProductFilter productFilter)
    {
        _logger = logger;
        _session = session;
        _productFilter = productFilter;
    }

    [Transaction]
    public async Task Handle(BillRequestAccepted @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received BillRequestAcceptedEvent with RCMBillId {RcmBillId} ", @event.RCMBillId);

        if (!_productFilter.ShouldProcess(@event.RCMProductCode))
        {
            _logger.LogInformation("Product code not found, bill accepted event ignored, for RCMBillId={RcmBillId}",
                @event.RCMBillId);
            return;
        }

        await _session.SendLocal(@event, cancellationToken: cancellationToken);
    }
}