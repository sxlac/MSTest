using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.FOBT.Svc.Core.Constants;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Sagas;

namespace Signify.FOBT.Svc.Core.EventHandlers;

public class InventoryUpdatedHandler : IHandleEvent<InventoryUpdated>
{
    private readonly IMessageSession _messageSession;
    private readonly ILogger<InventoryUpdatedHandler> _logger;
    private readonly IMapper _mapper;

    public InventoryUpdatedHandler(ILogger<InventoryUpdatedHandler> logger,
        IMessageSession messageSession, IMapper mapper)
    {
        _logger = logger;
        _messageSession = messageSession;
        _mapper = mapper;
    }

    [Transaction]
    public async Task Handle(InventoryUpdated evt, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Start Handle InventoryUpdated, ItemNumber: {ItemNumber}, Barcode: {SerialNumber}", evt.ItemNumber, evt.SerialNumber);

        //Check if FOBT item
        if (!evt.ItemNumber.Equals(ApplicationConstants.ItemNumber, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Task Completed. InventoryUpdate Ignored, ItemNumber:{ItemNumber}, ProviderId: {ProviderId}, Barcode:{SerialNumber}",
                evt.ItemNumber, evt.ProviderId, evt.SerialNumber);
            return;
        }

        var inventoryUpdateReceived = _mapper.Map<InvUpdateReceived>(evt);
        //publish NServiceBus event
        await _messageSession.Publish(inventoryUpdateReceived, cancellationToken: cancellationToken);

        _logger.LogDebug("End Handle InventoryUpdated, ItemNumber: {ItemNumber}, Barcode: {SerialNumber}", evt.ItemNumber, evt.SerialNumber);
    }
}