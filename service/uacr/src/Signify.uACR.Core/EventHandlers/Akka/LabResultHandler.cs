using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.uACR.Core.Events;
using Signify.uACR.Core.FeatureFlagging;
using Signify.uACR.Core.Infrastructure;

namespace Signify.uACR.Core.EventHandlers.Akka;

/// <summary>
/// Akka/Kafka event handler for the <see cref="KedUacrLabResult"/>
/// </summary>
public class LabResultHandler(
    ILogger<LabResultHandler> logger,
    IMessageSession messageSession,
    IApplicationTime applicationTime,
    IFeatureFlags featureFlags)
    : IHandleEvent<KedUacrLabResult>
{
    /// <summary>
    /// Send NSB event to handle the request
    /// </summary>
    /// <param name="event"></param>
    /// <param name="cancellationToken"></param>
    [Transaction]
    public async Task Handle(KedUacrLabResult @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received LabResult with EvaluationId={EvaluationId}", @event.EvaluationId);
        
        if (!featureFlags.EnableLabResultIngestion)
        {
            logger.LogInformation("Lab result ingestion is NOT enabled for EvaluationId={EvaluationId}", @event.EvaluationId);
            return;
        }
        @event.ReceivedByUacrDateTime = applicationTime.UtcNow();
        
        await messageSession.SendLocal(@event, cancellationToken: cancellationToken);
        logger.LogInformation("End Handle LabResult {EvaluationId}", @event.EvaluationId);
    }
}