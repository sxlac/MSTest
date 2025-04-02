using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Constants;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.Core.Filters;

namespace Signify.PAD.Svc.Core.EventHandlers
{
    /// <summary>
    ///This handles evaluation finalized event. It filters PAD products, DOS updates and raise PAD Received Event.
    /// </summary>
    public class EvaluationFinalizedHandler : IHandleEvent<EvaluationFinalizedEvent>
    {
        private readonly ILogger _logger;
        private readonly IMessageSession _messageSession;
        private readonly IMapper _mapper;
        private readonly IProductFilter _productFilter;
        private readonly IPublishObservability _publishObservability;
        
        public EvaluationFinalizedHandler(ILogger<EvaluationFinalizedHandler> logger,
            IMessageSession messageSession,
            IMapper mapper,
            IProductFilter productFilter,
            IPublishObservability publishObservability)
        {
            _logger = logger;
            _messageSession = messageSession;
            _mapper = mapper;
            _productFilter = productFilter;
            _publishObservability = publishObservability;
        }

        [Transaction]
        public async Task Handle(EvaluationFinalizedEvent @event, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Received EvaluationFinalizedEvent with {Count} product codes: {ProductCodes}, for EvaluationId={EvaluationId} with EventId={EventId}",
                @event.Products.Count, string.Join(',', @event.Products.Select(p => p.ProductCode)), @event.EvaluationId, @event.Id);

            if (!_productFilter.ShouldProcess(@event.Products))
            {
                _logger.LogDebug("Event ignored, for EvaluationId={EvaluationId} with EventId={EventId}", @event.EvaluationId, @event.Id);
                return;
            }

            var createPad = _mapper.Map<CreatePad>(@event);
			await _messageSession.SendLocal(createPad, cancellationToken);

            _logger.LogInformation("Event queued for processing, for EvaluationId={EvaluationId} with EventId={EventId}", @event.EvaluationId, @event.Id);
            
            PublishObservability(@event, Observability.Evaluation.EvaluationFinalizedEvent, sendImmediate: true);
        }	
        
        private void PublishObservability(EvaluationFinalizedEvent message, string eventType, bool sendImmediate = false)
        {
            var observabilityFinalizedEvent = new ObservabilityEvent
            {
                EvaluationId = message.EvaluationId,
                EventType = eventType,
                EventValue = new Dictionary<string, object>
                {
                    { Observability.EventParams.EvaluationId, message.EvaluationId },
                    { Observability.EventParams.CreatedDateTime, message.CreatedDateTime.ToUnixTimeSeconds() }
                }
            };

            _publishObservability.RegisterEvent(observabilityFinalizedEvent, sendImmediate);
        }
    }
}