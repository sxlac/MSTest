using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using SpiroEvents;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.EventHandlers.Akka
{
    /// <summary>
    /// Akka/Kafka event handler for the <see cref="OverreadProcessed"/> event
    /// </summary>
    /// <remarks>
    /// This inbound event named <see cref="OverreadProcessed"/> may not be the best name here
    /// within the context of this process manager. The event is named this way because the <i>vendor</i>
    /// has processed an overread; it is not to be confused in thinking the overread has been
    /// processed by the <i>spirometry process manager</i>. To the contrary, this is what triggers
    /// the diagnosis loopback within this process manager.
    /// </remarks>
    public class OverreadProcessedByVendorHandler : IHandleEvent<OverreadProcessed>
    {
        private readonly ILogger _logger;
        private readonly IMessageSession _session;

        public OverreadProcessedByVendorHandler(ILogger<OverreadProcessedByVendorHandler> logger,
            IMessageSession session)
        {
            _logger = logger;
            _session = session;
        }

        [Transaction]
        public async Task Handle(OverreadProcessed @event, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Received OverreadProcessed event from vendor with ExternalId={ExternalId}, for AppointmentId={AppointmentId}", @event.OverreadId, @event.AppointmentId);

            await _session.SendLocal(@event);

            _logger.LogInformation("Event queued for processing, for AppointmentId={AppointmentId}", @event.AppointmentId);
        }
    }
}
