using System.Collections.Generic;
using AutoMapper;
using FobtNsbEvents;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Filters;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Signify.FOBT.Svc.Core.Constants;
using Signify.FOBT.Svc.Core.Infrastructure.Observability;

namespace Signify.FOBT.Svc.Core.EventHandlers;

/// <summary>
/// This handles evaluation finalized event. It filters FOBT products and raise Endorse EvaluationFinalized Event.
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
        using var _ = _logger.BeginScope("EventId={EventId}, EvaluationId={EvaluationId}",
            @event.Id, @event.EvaluationId);

        _logger.LogInformation("Received EvaluationFinalizedEvent with {Count} product codes: {ProductCodes} for Evaluation Id:{EvaluationId}",
            @event.Products.Count, string.Join(',', @event.Products.Select(p => p.ProductCode)), @event.EvaluationId);

        if (!_productFilter.ShouldProcess(@event.Products))
        {
            _logger.LogInformation("Event ignored");
            return;
        }

        var fobtEvalReceived = _mapper.Map<FobtEvalReceived>(@event);
        await _messageSession.SendLocal(fobtEvalReceived, cancellationToken: cancellationToken);

        _logger.LogInformation("Event queued for processing");
            
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