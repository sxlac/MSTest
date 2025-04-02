using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.CKD.Messages.Events;
using Signify.CKD.Sagas;
using Signify.CKD.Svc.Core.Constants;

namespace Signify.CKD.Svc.Core.EventHandlers
{
	public class InventoryUpdatedHandler : IHandleEvent<InventoryUpdated>
	{
		private readonly ILogger _logger;
		private readonly IMessageSession _messageSession;
		private readonly IMapper _mapper;

		public InventoryUpdatedHandler(ILogger<InventoryUpdatedHandler> logger,
			IMessageSession messageSession,
			IMapper mapper)
		{
			_logger = logger;
			_messageSession = messageSession;
			_mapper = mapper;
		}

		[Transaction]
		public async Task Handle(InventoryUpdated @event, CancellationToken cancellationToken)
		{
			_logger.LogInformation("Received inventory updated event, with ItemNumber={ItemNumber}, SerialNumber={SerialNumber}, RequestId={RequestId}",
				@event.ItemNumber, @event.SerialNumber, @event.RequestId);

			if (!@event.ItemNumber.Equals(ConnectionStringNames.ItemNumber, StringComparison.OrdinalIgnoreCase))
			{
				_logger.LogDebug("Event ignored, for SerialNumber={SerialNumber}", @event.SerialNumber);
				return;
			}

			var inventoryUpdateReceived = _mapper.Map<InventoryUpdateReceived>(@event);
			await _messageSession.SendLocal(inventoryUpdateReceived);

			_logger.LogInformation("Event queued for process, for SerialNumber={SerialNumber}", @event.SerialNumber);
		}
	}
}
