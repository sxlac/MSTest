using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.CKD.Svc.Core.Constants;
using Signify.CKD.Svc.Core.Events;
using Signify.CKD.Svc.Core.Filters;
using Signify.CKD.Svc.Core.Infrastructure.Observability;

namespace Signify.CKD.Svc.Core.EventHandlers;

public class PdfDeliveredHandler : IHandleEvent<PdfDeliveredToClient>
{
    private readonly ILogger _logger;
    private readonly IProductFilter _productFilter;
    private readonly IMessageSession _messageSession;
    private readonly IObservabilityService _observabilityService;
    
    public PdfDeliveredHandler(ILogger<PdfDeliveredHandler> logger,
        IProductFilter productFilter,
        IMessageSession messageSession,
        IObservabilityService observabilityService)
    {
        _logger = logger;
        _productFilter = productFilter;
        _messageSession = messageSession;
        _observabilityService = observabilityService;
    }

    public async Task Handle(PdfDeliveredToClient @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received pdf delivery event with {Count} product codes: {ProductCodes}, for EvaluationId={EvaluationId} with EventId={EventId}",
            @event.ProductCodes.Count, string.Join(',', @event.ProductCodes), @event.EvaluationId, @event.EventId);

        if (!_productFilter.ShouldProcess(@event.ProductCodes))
        {
            _logger.LogDebug("Event ignored, for EvaluationId={EvaluationId} with EventId={EventId}", @event.EvaluationId, @event.EventId);
            return;
        }

        await _messageSession.SendLocal(@event);

        _logger.LogInformation("Event queued for processing, for EvaluationId={EvaluationId} with EventId={EventId}", @event.EvaluationId, @event.EventId);
        
        _observabilityService.AddEvent(Observability.PdfDelivered.PdfDeliveryReceivedEvent, new Dictionary<string, object>
        {
            {Observability.EventParams.EvaluationId, @event.EvaluationId},
            {Observability.EventParams.CreatedDateTime, ((DateTimeOffset)@event.CreatedDateTime).ToUnixTimeSeconds()}
        });
    }
}