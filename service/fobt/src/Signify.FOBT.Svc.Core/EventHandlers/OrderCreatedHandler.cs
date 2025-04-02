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
///     Subscribes to OrderCreated kafka event published by Labs API and kicks off barcode update
///     Refer workflow in: https://jira.signifyhealth.com/browse/ANC-893
/// </summary>
public class OrderCreatedHandler : IHandleEvent<BarcodeUpdate>
{
    private readonly ILogger<OrderCreatedHandler> _logger;
    private readonly IMessageSession _messageSession;
    private readonly IProductFilter _productFilter;

    public OrderCreatedHandler(ILogger<OrderCreatedHandler> logger,
        IMessageSession messageSession,
        IProductFilter productFilter)
    {
        _logger = logger;
        _messageSession = messageSession;
        _productFilter = productFilter;
    }

    [Transaction]
    public async Task Handle(BarcodeUpdate @event, CancellationToken cancellationToken)
    {
        if (!_productFilter.ShouldProcess(new[] {@event.ProductCode}))
        {
            _logger.LogInformation("Ignoring event for EvaluationId {EvaluationId}, with ProductCode {ProductCode}, Barcode {Barcode}, OrderCorrelationId {OrderCorrelationId}",
                @event.EvaluationId, @event.ProductCode, @event.Barcode, @event.OrderCorrelationId);

            return;
        }

        await _messageSession.SendLocal(@event, cancellationToken: cancellationToken);

        _logger.LogInformation("Enqueued event for processing, for EvaluationId {EvaluationId}, ProductCode {ProductCode} Barcode {Barcode}, OrderCorrelationId {OrderCorrelationId}",
            @event.EvaluationId, @event.ProductCode, @event.Barcode, @event.OrderCorrelationId);
    }
}