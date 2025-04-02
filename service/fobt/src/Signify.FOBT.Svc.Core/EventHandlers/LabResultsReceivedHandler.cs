using FobtNsbEvents;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.FOBT.Svc.Core.Filters;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.FOBT.Svc.Core.EventHandlers;

/// <summary>
///This handles BarCodeUpdated event.
/// </summary>
public class LabResultsReceivedHandler : IHandleEvent<HomeAccessResultsReceived>
{
    private readonly ILogger _logger;
    private readonly IMessageSession _session;
    private readonly IProductFilter _productFilter;

    public LabResultsReceivedHandler(ILogger<LabResultsReceivedHandler> logger,
        IMessageSession session,
        IProductFilter productFilter)
    {
        _logger = logger;
        _session = session;
        _productFilter = productFilter;
    }

    [Transaction]
    public async Task Handle(HomeAccessResultsReceived @event, CancellationToken cancellationToken)
    {
        using var scope = _logger.BeginScope("EventId={EventId}, Barcode={Barcode}, OrderCorrelationId={OrderCorrelationId}",
            @event.EventId, @event.Barcode, @event.OrderCorrelationId);

        _logger.LogInformation("Received HomeAccessResultsReceived event with LabTestType {LabTestType}", @event.LabTestType);

        if (!_productFilter.ShouldProcess(new []{@event.LabTestType}))
        {
            _logger.LogInformation("Event ignored");
            return;
        }

        await _session.SendLocal(@event, cancellationToken: cancellationToken);

        _logger.LogInformation("Event queued for processing");
    }
}