using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.DEE.Svc.Core.Events;
using Signify.DEE.Svc.Core.Filters;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.EventHandlers.Akka
{
    /// <summary>
    /// Akka/Kafka event handler for the <see cref="CDIEvaluationHoldReleasedEvent"/>
    /// </summary>
    public class EvaluationHoldReleasedHandler : IHandleEvent<CDIEvaluationHoldReleasedEvent>
    {
        private readonly ILogger _logger;
        private readonly IMessageSession _session;
        private readonly IProductFilter _productFilter;

        public EvaluationHoldReleasedHandler(ILogger<EvaluationHoldReleasedHandler> logger,
            IMessageSession session,
            IProductFilter productFilter)
        {
            _logger = logger;
            _session = session;
            _productFilter = productFilter;
        }

        [Transaction]
        public async Task Handle(CDIEvaluationHoldReleasedEvent @event, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Received CDIEvaluationHoldReleasedEvent with {Count} product codes: {ProductCodes}, for EvaluationId={EvaluationId} with EventId={EventId}",
                @event.Products.Count, string.Join(',', @event.Products.Select(p => p.Code)), @event.EvaluationId, @event.EventId);

            if (!_productFilter.ShouldProcess(@event.Products))
            {
                _logger.LogDebug("CDIEvaluationHoldReleasedEvent ignored, for EvaluationId={EvaluationId} with EventId={EventId}", @event.EvaluationId, @event.EventId);
                return;
            }

            await _session.SendLocal(@event, cancellationToken);

            _logger.LogInformation("CDIEvaluationHoldReleasedEvent queued for processing, for EvaluationId={EvaluationId} with EventId={EventId}", @event.EvaluationId, @event.EventId);
        }
    }
}
