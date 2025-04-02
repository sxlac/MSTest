using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Filters;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.FOBT.Svc.Core.EventHandlers;

/// <summary>
///     Subscribes to OrderHeld kafka event published by Labs API and publishes OrderHeld event.
///     Refer workflow in: https://cvs-hcd.atlassian.net/browse/ANC-6355
/// </summary>
public class OrderHeldHandler : IHandleEvent<OrderHeld>
{
    private readonly ILogger<OrderHeldHandler> _logger;
    private readonly IMessageSession _messageSession;
    private readonly IProductFilter _productFilter;

    public OrderHeldHandler(ILogger<OrderHeldHandler> logger,
        IMessageSession messageSession,
        IProductFilter productFilter)
    {
        _logger = logger;
        _messageSession = messageSession;
        _productFilter = productFilter;
    }

    [Transaction]
    public async Task Handle(OrderHeld @event, CancellationToken cancellationToken)
    {
        if (!_productFilter.ShouldProcess(new[] { @event.ProductCode }))
        {
            _logger.LogInformation("Ignoring event for Barcode {Barcode}, with ProductCode {ProductCode}",
                @event.Barcode, @event.ProductCode);

            return;
        }

        await _messageSession.SendLocal(@event, cancellationToken: cancellationToken);

        _logger.LogInformation("Enqueued event for processing, for Barcode {Barcode}, ProductCode {ProductCode}",
            @event.Barcode, @event.ProductCode);
    }
}