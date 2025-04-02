using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Signify.PAD.Svc.Core.Events;
using Signify.AkkaStreams.Kafka;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Svc.Core.Constants;

namespace Signify.PAD.Svc.Core.EventHandlers
{
    public class PdfDeliveredHandler : IHandleEvent<PdfDeliveredToClient>
    {
        private readonly ILogger _logger;
        private readonly IMessageSession _messageSession;
        private readonly IPublishObservability _publishObservability;
        public PdfDeliveredHandler(
            ILogger<PdfDeliveredHandler> logger, 
            IMessageSession messageSession,
            IPublishObservability publishObservability)
        {
            _logger = logger;
            _messageSession = messageSession;
            _publishObservability = publishObservability;
        }

        public async Task Handle(PdfDeliveredToClient @event, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Start Handle PdfDelivered, EvaluationID: {EvaluationId}, EventId: {EventId}", @event.EvaluationId, @event.EventId);
            var isPadProduct = @event.ProductCodes.Any(p =>
                string.Equals(p, Application.ProductCode, StringComparison.OrdinalIgnoreCase));

            if (!isPadProduct)
            {
                _logger.LogInformation("Product code is not PAD. Evaluation Ignored, EvaluationID: {EvaluationId}", @event.EvaluationId);
                return;
            }

            _logger.LogInformation("Evaluation Identified with product code PAD, EvaluationID : {EvaluationId}, EventId: {EventId}",
                @event.EvaluationId, @event.EventId);

            await _messageSession.SendLocal(@event, cancellationToken);
            
            _logger.LogInformation("Event queued for processing, for EvaluationId={EvaluationId} with EventId={EventId}", @event.EvaluationId, @event.EventId);
            
            PublishObservability(@event, Observability.PdfDelivered.PdfDeliveryReceivedEvent, sendImmediate: true);
        }
        private void PublishObservability(PdfDeliveredToClient message, string eventType, bool sendImmediate = false)
        {
            var observabilityPdfDeliveryReceivedEvent = new ObservabilityEvent
            {
                EvaluationId = message.EvaluationId,
                EventType = eventType,
                EventValue = new Dictionary<string, object>
                {
                    {Observability.EventParams.EvaluationId, message.EvaluationId},
                    {Observability.EventParams.CreatedDateTime, ((DateTimeOffset)message.CreatedDateTime).ToUnixTimeSeconds()}
                }
            };

            _publishObservability.RegisterEvent(observabilityPdfDeliveryReceivedEvent, sendImmediate);
        }
    }
}