using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.Spirometry.Core.Filters;
using SpiroEvents;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.EventHandlers.Akka
{
    /// <summary>
    /// Akka/Kafka event handler for the <see cref="PdfDeliveredToClient"/>
    /// </summary>
    public class PdfDeliveredToClientHandler : IHandleEvent<PdfDeliveredToClient>
    {
        private readonly ILogger _logger;

        private readonly IMessageSession _session;
        private readonly IProductFilter _productFilter;

        public PdfDeliveredToClientHandler(ILogger<PdfDeliveredToClientHandler> logger,
            IMessageSession session,
            IProductFilter productFilter)
        {
            _logger = logger;
            _session = session;
            _productFilter = productFilter;
        }

        [Transaction]
        public async Task Handle(PdfDeliveredToClient @event, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Received PdfDeliveredToClient with {Count} product codes: {ProductCodes}, for EvaluationId={EvaluationId} with EventId={EventId}",
                @event.ProductCodes.Count, string.Join(',', @event.ProductCodes), @event.EvaluationId, @event.EventId);

            if (!_productFilter.ShouldProcess(@event.ProductCodes))
            {
                _logger.LogInformation("Event ignored, for EvaluationId={EvaluationId} with EventId={EventId}", @event.EvaluationId, @event.EventId);
                return;
            }

            await _session.SendLocal(@event);

            _logger.LogInformation("Event queued for processing, for EvaluationId={EvaluationId} with EventId={EventId}", @event.EvaluationId, @event.EventId);
        }
    }
}
