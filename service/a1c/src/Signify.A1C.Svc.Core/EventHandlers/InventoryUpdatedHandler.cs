using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.A1C.Svc.Core.Events;
using Signify.A1C.Svc.Core.Sagas;
using Signify.AkkaStreams.Kafka;

namespace Signify.A1C.Svc.Core.EventHandlers
{
    public class InventoryUpdatedHandler : IHandleEvent<InventoryUpdated>
    {

        private readonly IEndpointInstance _endpoint;
        private readonly ILogger<InventoryUpdatedHandler> _logger;
        private readonly IMapper _mapper;

        public InventoryUpdatedHandler(ILogger<InventoryUpdatedHandler> logger, IEndpointInstance endpoint, IMapper mapper)
        {
            _logger = logger;
            _endpoint = endpoint;
            _mapper = mapper;

        }

        [Transaction]
        public async Task Handle(InventoryUpdated @event, CancellationToken cancellationToken)
        {
            _logger.LogDebug(
                $"Start Handle InventoryUpdated, ItemNumber: {@event.ItemNumber}, Barcode: {@event.SerialNumber}");

            //Check if A1C item
            if (!@event.ItemNumber.Equals(Constants.ItemNumber, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug(
                    $"Task Completed. InventoryUpdate Ignored, ItemNumber:{@event.ItemNumber}, ProviderId: {@event.ProviderId}, Barcode:{@event.SerialNumber}");
                return;
            }

            var inventoryUpdateReceived = _mapper.Map<InventoryUpdateReceived>(@event);
            //publish NServiceBus event
            await _endpoint.Publish(inventoryUpdateReceived);

            _logger.LogDebug($"End Handle InventoryUpdated, ItemNumber: {@event.ItemNumber}, Barcode: {@event.SerialNumber}");

        }
    }
}
