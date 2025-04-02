using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.DEE.Svc.Core.Constants;
using Signify.DEE.Svc.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;

namespace Signify.DEE.Svc.Core.EventHandlers;

public class EvaluationFinalizedEventHandler(
    ILogger<EvaluationFinalizedEventHandler> log,
    IMessageSession endpoint,
    IPublishObservability publishObservability)
    : IHandleEvent<EvaluationFinalizedEvent>
{
    [Transaction]
    public async Task Handle(EvaluationFinalizedEvent message, CancellationToken cancellationToken)
    {
        if (message.Products == null || message.Products.Any(x => x == null))
        {
            log.LogDebug("Evaluation:{EvaluationId} -- Found no {ProductCode} product code in FinalizeEvent payload", message.EvaluationId, Constants.ApplicationConstants.ProductCode);
            return;
        }

        log.LogInformation("EvaluationId: {EvaluationId} has {ProductCount} product(s): {ProductCode}", message.EvaluationId, message.Products.Count, string.Join(",", message.Products.Select(p => p.ProductCode)));
        log.LogDebug("System expecting product code: {ProductCode}", ApplicationConstants.ProductCode);

        if (!message.Products.Any(x => x.ProductCode.Equals(ApplicationConstants.ProductCode, StringComparison.OrdinalIgnoreCase)))
        {
            log.LogInformation("Evaluation:{EvaluationId} -- Found no {ProductCode} product code found in FinalizeEvent payload.", message.EvaluationId, ApplicationConstants.ProductCode);
            return;
        }

        await endpoint.SendLocal(message, cancellationToken).ConfigureAwait(false);

        log.LogInformation("Event queued for processing, for EvaluationId={EvaluationId} with EventId={EventId}", message.EvaluationId, message.Id);
        PublishObservability(message, Observability.Evaluation.EvaluationFinalizedEvent);
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

        log.LogDebug("End Handle EvaluationFinalized");
    }
}