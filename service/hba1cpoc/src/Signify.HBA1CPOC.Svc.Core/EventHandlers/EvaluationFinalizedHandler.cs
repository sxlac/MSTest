using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Signify.HBA1CPOC.Svc.Core.Events;
using System.Threading;
using System.Threading.Tasks;
using NewRelic.Api.Agent;
using Signify.AkkaStreams.Kafka;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Constants;
using Signify.HBA1CPOC.Svc.Core.Infrastructure.Observability;

namespace Signify.HBA1CPOC.Svc.Core.EventHandlers;

/// <summary>
///This handles the evaluation finalized event. It filters HBA1CPOC products and raise HBA1CPOC Received Event.
/// </summary>
public class EvaluationFinalizedHandler : IHandleEvent<EvaluationFinalizedEvent>
{
    private readonly ILogger _logger;
    private readonly IMessageSession _session;
    private readonly IMapper _mapper;
    private readonly IPublishObservability _publishObservability;
    
    public EvaluationFinalizedHandler(ILogger<EvaluationFinalizedHandler> logger,
        IMessageSession session, 
        IMapper mapper, 
        IPublishObservability publishObservability)
    {
        _logger = logger;
        _session = session;
        _mapper = mapper;
        _publishObservability = publishObservability;
    }

    [Transaction]
    public async Task Handle(EvaluationFinalizedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received EvaluationFinalizedEvent with {Count} product codes: {ProductCodes}, for EvaluationId={EvaluationId} with EventId={EventId}",
            @event.Products.Count, string.Join(',', @event.Products.Select(p => p.ProductCode)), @event.EvaluationId,
            @event.Id);

        var shouldProcess = @event.Products.Any(p =>
            string.Equals(p.ProductCode, ApplicationConstants.ProductCode,
                StringComparison.OrdinalIgnoreCase));

        if (!shouldProcess)
        {
            _logger.LogInformation("Event ignored, for EvaluationId={EvaluationId} with EventId={EventId}",
                @event.EvaluationId, @event.Id);
            return;
        }

        var command = _mapper.Map<CreateHbA1CPoc>(@event);
        await _session.SendLocal(command, cancellationToken);

        _logger.LogInformation("Event queued for processing, for EvaluationId={EvaluationId} with EventId={EventId}",
            @event.EvaluationId, @event.Id);
        
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