using System.Collections.Generic;
using AutoMapper;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.Spirometry.Core.Events.Akka;
using Signify.Spirometry.Core.Filters;
using Signify.Spirometry.Core.Infrastructure;
using SpiroNsbEvents;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.Spirometry.Core.Constants;

namespace Signify.Spirometry.Core.EventHandlers.Akka;

/// <summary>
/// Akka/Kafka event handler for the <see cref="EvaluationFinalizedEvent"/>
/// </summary>
public class EvaluationFinalizedHandler : IHandleEvent<EvaluationFinalizedEvent>
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IMessageSession _session;
    private readonly IApplicationTime _applicationTime;
    private readonly IProductFilter _productFilter;
    private readonly IPublishObservability _publishObservability;
    
    public EvaluationFinalizedHandler(ILogger<EvaluationFinalizedHandler> logger,
        IMapper mapper,
        IMessageSession session,
        IApplicationTime applicationTime,
        IProductFilter productFilter,
        IPublishObservability publishObservability)
    {
        _logger = logger;
        _mapper = mapper;
        _session = session;
        _applicationTime = applicationTime;
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

        // Technically could be slightly more accurate getting this at the start of this method, but this way avoids the
        // overhead on every message (many of which won't contain a Spirometry product), and the filtering is fast enough.
        var now = _applicationTime.UtcNow();

        var evalReceived = _mapper.Map<EvalReceived>(@event);
        evalReceived.ReceivedBySpirometryProcessManagerDateTime = now;

        await _session.SendLocal(evalReceived);

        _logger.LogInformation("Event queued for processing, for EvaluationId={EvaluationId} with EventId={EventId}", @event.EvaluationId, @event.Id);
        
        PublishObservability(@event, Observability.Evaluation.EvaluationFinalizedEvent);
    }
    private void PublishObservability(EvaluationFinalizedEvent message, string eventType)
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

        _publishObservability.RegisterEvent(observabilityFinalizedEvent, true);
    }
}