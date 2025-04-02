using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Filters;
using System.Threading;
using System.Threading.Tasks;
using Signify.FOBT.Svc.Core.Constants;
using Signify.FOBT.Svc.Core.Infrastructure.Observability;

namespace Signify.FOBT.Svc.Core.EventHandlers;

public class PdfDeliveredHandler : IHandleEvent<PdfDeliveredToClient>
{
    private readonly ILogger _logger;
    private readonly IMessageSession _messageSession;
    private readonly IProductFilter _productFilter;
    private readonly IPublishObservability _publishObservability;
        
    public PdfDeliveredHandler(ILogger<PdfDeliveredHandler> logger,
        IMessageSession messageSession,
        IProductFilter productFilter,
        IPublishObservability publishObservability)
    {
        _logger = logger;
        _messageSession = messageSession;
        _productFilter = productFilter;
        _publishObservability = publishObservability;
    }

    public async Task Handle(PdfDeliveredToClient pdfDeliveredToClientEvent, CancellationToken cancellationToken)
    {
        using var _ = _logger.BeginScope("EventId={EventId}, EvaluationId={EvaluationId}",
            pdfDeliveredToClientEvent.EventId, pdfDeliveredToClientEvent.EvaluationId);

        _logger.LogInformation("Received PdfDeliveredToClient with {Count} product codes: {ProductCodes}",
            pdfDeliveredToClientEvent.ProductCodes.Count, string.Join(',', pdfDeliveredToClientEvent.ProductCodes));

        if (!_productFilter.ShouldProcess(pdfDeliveredToClientEvent.ProductCodes))
        {
            _logger.LogInformation("Event ignored");
            return;
        }

        await _messageSession.SendLocal(pdfDeliveredToClientEvent, cancellationToken: cancellationToken);

        _logger.LogInformation("Event queued for processing");
            
        PublishObservability(pdfDeliveredToClientEvent, Observability.PdfDelivered.PdfDeliveryReceivedEvent);
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