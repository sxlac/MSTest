using System;
using System.Collections.Generic;
using Akka.Streams.Kafka.Extensions;
using Microsoft.Extensions.Logging;

namespace Signify.FOBT.Svc.Core.Infrastructure.Observability;

public class PublishObservability : IPublishObservability
{
    private readonly ILogger<PublishObservability> _logger;
    private readonly List<ObservabilityEvent> _observabilityEvents;
    private readonly IObservabilityService _observabilityService;

    public PublishObservability(ILogger<PublishObservability> logger, IObservabilityService observabilityService)
    {
        _logger = logger;
        _observabilityService = observabilityService;
        _observabilityEvents = new List<ObservabilityEvent>();
    }

    /// <inheritdoc />
    public void RegisterEvent(ObservabilityEvent observabilityEvent, bool sendImmediately = false)
    {
        if (observabilityEvent == null)
            return;
        
        if (sendImmediately)
        {
            _observabilityService.AddEvent(observabilityEvent.EventType, observabilityEvent.EventValue);
        }
        else
        {
            _observabilityEvents.Add(observabilityEvent);
        }
    }

    /// <inheritdoc />
    public void Commit()
    {
        if (_observabilityEvents.IsEmpty())
            return;
        
        foreach (var status in _observabilityEvents)
        {
            try
            {
                _observabilityService.AddEvent(status.EventType, status.EventValue);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "Exception in {Handler} while adding observability for EvaluationId={EvaluationId}, EventId={EventId}",
                    nameof(PublishObservability), status.EvaluationId, status.EventId);
            }
        }
        _observabilityEvents.Clear();
    }
}