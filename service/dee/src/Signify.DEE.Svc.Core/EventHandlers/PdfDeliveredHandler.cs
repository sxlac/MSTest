using AutoMapper;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.DEE.Svc.Core.Commands;
using Signify.DEE.Svc.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Signify.DEE.Svc.Core.Constants;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;

namespace Signify.DEE.Svc.Core.EventHandlers;

public class PdfDeliveredHandler(
    ILogger<PdfDeliveredHandler> logger,
    IMapper mapper,
    IMessageSession endpoint,
    IPublishObservability publishObservability)
    : IHandleEvent<PdfDeliveredToClient>
{
    public async Task Handle(PdfDeliveredToClient @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received PdfDeliveredToClient with {Count} product codes: {ProductCodes}, for EvaluationId={EvaluationId} with EventId={EventId}",
            @event.ProductCodes.Count, string.Join(',', @event.ProductCodes), @event.EvaluationId, @event.EventId);
            
        if (!HasRelevantProduct(@event))
        {
            logger.LogInformation("Event ignored, for EvaluationId={EvaluationId} with EventId={EventId}", @event.EvaluationId, @event.EventId);
            return;
        }

        var processPdf = mapper.Map<ProcessPdfDelivered>(@event);
        await endpoint.SendLocal(processPdf, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Event queued for processing, for EvaluationId={EvaluationId} with EventId={EventId}", @event.EvaluationId, @event.EventId);
            
        PublishObservability(@event, Observability.PdfDelivered.PdfDeliveryReceivedEvent);
    }

    private static bool HasRelevantProduct(PdfDeliveredToClient @event)
    {
        return @event.ProductCodes.Any(p =>
            string.Equals(p, ApplicationConstants.ProductCode, StringComparison.OrdinalIgnoreCase));
    }
        
    private void PublishObservability(PdfDeliveredToClient message, string eventType)
    {
        var observabilityPdfDeliveryReceivedEvent = new ObservabilityEvent
        {
            EvaluationId = (int) message.EvaluationId,
            EventType = eventType,
            EventValue = new Dictionary<string, object>
            {
                { Observability.EventParams.EvaluationId, message.EvaluationId },
                { Observability.EventParams.CreatedDateTime, ((DateTimeOffset)message.CreatedDateTime).ToUnixTimeSeconds() }
            }
        };

        publishObservability.RegisterEvent(observabilityPdfDeliveryReceivedEvent, true);
    }
}