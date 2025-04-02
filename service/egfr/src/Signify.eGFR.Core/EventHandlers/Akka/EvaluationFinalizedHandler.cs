using System.Collections.Generic;
using AutoMapper;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.eGFR.Core.Events.Akka;
using Signify.eGFR.Core.Filters;
using Signify.eGFR.Core.Infrastructure;
using EgfrNsbEvents;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.eGFR.Core.Constants;

namespace Signify.eGFR.Core.EventHandlers.Akka;

/// <summary>
/// Akka/Kafka event handler for the <see cref="EvaluationFinalizedEvent"/>
/// </summary>
public class EvaluationFinalizedHandler(
    ILogger<EvaluationFinalizedHandler> logger,
    IMapper mapper,
    IMessageSession message,
    IApplicationTime applicationTime,
    IProductFilter productFilter,
    IPublishObservability publishObservability)
    : IHandleEvent<EvaluationFinalizedEvent>
{
    private readonly ILogger _logger = logger;

    [Transaction]
    public async Task Handle(EvaluationFinalizedEvent @event, CancellationToken cancellationToken)
    {
        var shouldProcess = productFilter.ShouldProcess(@event.Products);

        _logger.LogInformation(
            "{Processing} EvaluationFinalizedEvent with {Count} product codes: {ProductCodes}, for EventId={EventId}, EvaluationId={EvaluationId}",
            shouldProcess ? "Processing" : "Ignoring",
            @event.Products.Count, string.Join(',', @event.Products.Select(p => p.ProductCode)), @event.Id, @event.EvaluationId);

        if (!shouldProcess)
            return;

        // Technically could be slightly more accurate getting this at the start of this method, but this way avoids the
        // overhead on every message (many of which won't contain a eGFR product), and the filtering is fast enough.
        var evalReceived = mapper.Map<EvalReceived>(@event);
        evalReceived.ReceivedByeGFRProcessManagerDateTime = applicationTime.UtcNow();

        await message.SendLocal(evalReceived, cancellationToken: cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Event queued for processing");

        PublishObservability(@event, Observability.Evaluation.EvaluationFinalizedEvent);
    }

    private void PublishObservability(EvaluationFinalizedEvent message, string eventType)
    {
        //add to dps dashboard
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