using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.HBA1CPOC.Messages.Events;
using Signify.HBA1CPOC.Sagas;
using Signify.HBA1CPOC.Svc.Core.Constants;

namespace Signify.HBA1CPOC.Svc.Core.EventHandlers;

public class InventoryUpdatedHandler : IHandleEvent<InventoryUpdated>
{
    private readonly ILogger _logger;
    private readonly IMessageSession _endpoint;
    private readonly IMapper _mapper;

    public InventoryUpdatedHandler(ILogger<InventoryUpdatedHandler> logger,
        IMessageSession endpoint,
        IMapper mapper)
    {
        _logger = logger;
        _endpoint = endpoint;
        _mapper = mapper;
    }

    [Transaction]
    public async Task Handle(InventoryUpdated @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received InventoryUpdated, for ItemNumber={ItemNumber} and Barcode={Barcode} with RequestId={RequestId}",
            @event.ItemNumber, @event.SerialNumber, @event.RequestId);

        //Check if HBA1CPOC item
        if (!@event.ItemNumber.Equals(ApplicationConstants.ItemNumber, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "Event ignored, for ItemNumber={ItemNumber} and Barcode={Barcode} with RequestId={RequestId}",
                @event.ItemNumber, @event.SerialNumber, @event.RequestId);
            return;
        }

        //publish NServiceBus event
        var inventoryUpdateReceived = _mapper.Map<InventoryUpdateReceived>(@event);
        await _endpoint.Publish(inventoryUpdateReceived);
    }
}