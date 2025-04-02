using System.Collections.Generic;
using AutoMapper;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.uACR.Core.Events.Akka;
using Signify.uACR.Core.Filters;
using Signify.uACR.Core.Infrastructure;
using UacrNsbEvents;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.uACR.Core.Constants;

namespace Signify.uACR.Core.EventHandlers.Akka;

/// <summary>
/// Akka/Kafka event handler for the <see cref="EvaluationFinalizedEvent"/>
/// </summary>
public class EvaluationFinalizedHandler(
    ILogger<EvaluationFinalizedHandler> logger,
    IMapper mapper,
    IMessageSession messageSession,
    IApplicationTime applicationTime,
    IProductFilter productFilter,
    IPublishObservability publishObservability)
    : IHandleEvent<EvaluationFinalizedEvent>
{
    [Transaction]
    public async Task Handle(EvaluationFinalizedEvent @event, CancellationToken cancellationToken)
    {
        var shouldProcess = productFilter.ShouldProcess(@event.Products);

        logger.LogInformation(
            "{Processing} EvaluationFinalizedEvent with {Count} product codes: {ProductCodes}, for EventId={EventId}, EvaluationId={EvaluationId}",
            shouldProcess ? "Processing" : "Ignoring",
            @event.Products.Count, string.Join(',', @event.Products.Select(p => p.ProductCode)), @event.Id, @event.EvaluationId);

        if (!shouldProcess)
            return;

        // Technically could be slightly more accurate getting this at the start of this method, but this way avoids the
        // overhead on every message (many of which won't contain a uACR product), and the filtering is fast enough.
        var evalReceived = mapper.Map<EvalReceived>(@event);
        evalReceived.ReceivedByUacrProcessManagerDateTime = applicationTime.UtcNow();

        await messageSession.SendLocal(evalReceived, cancellationToken: cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Event queued for processing");

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

        publishObservability.RegisterEvent(observabilityFinalizedEvent, true);
    }
}