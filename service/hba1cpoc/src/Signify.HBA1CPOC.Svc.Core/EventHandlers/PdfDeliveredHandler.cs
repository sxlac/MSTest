using Microsoft.Extensions.Logging;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Signify.HBA1CPOC.Messages.Events;
using Signify.HBA1CPOC.Svc.Core.Constants;
using Signify.HBA1CPOC.Svc.Core.Infrastructure.Observability;

namespace Signify.HBA1CPOC.Svc.Core.EventHandlers;

public class PdfDeliveredHandler : IHandleEvent<PdfDeliveredToClient>
{
    private readonly ILogger _logger;
    private readonly IMessageSession _session;
    private readonly IPublishObservability _publishObservability;
    
    public PdfDeliveredHandler(ILogger<PdfDeliveredHandler> logger, 
        IMessageSession session,
        IPublishObservability publishObservability)
    {
        _logger = logger;
        _session = session;
        _publishObservability = publishObservability;
    }

    public async Task Handle(PdfDeliveredToClient @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received PdfDeliveredToClient with {Count} product codes: {ProductCodes}, for EvaluationId={EvaluationId} with EventId={EventId}",
            @event.ProductCodes.Count, string.Join(',', @event.ProductCodes), @event.EvaluationId, @event.EventId);

        var shouldProcess = @event.ProductCodes.Any(p =>
            string.Equals(p, ApplicationConstants.ProductCode, StringComparison.OrdinalIgnoreCase));

        if (!shouldProcess)
        {
            _logger.LogInformation("Event ignored, for EvaluationId={EvaluationId} with EventId={EventId}",
                @event.EvaluationId, @event.EventId);
            return;
        }

        await _session.SendLocal(@event);
        
        PublishObservability(@event, Observability.PdfDelivered.PdfDeliveryReceivedEvent);
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

        _publishObservability.RegisterEvent(observabilityPdfDeliveryReceivedEvent, true);
    }
}