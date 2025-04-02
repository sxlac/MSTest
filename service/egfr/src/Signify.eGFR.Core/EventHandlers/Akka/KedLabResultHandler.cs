using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.eGFR.Core.Events;
using Signify.eGFR.Core.FeatureFlagging;
using Signify.eGFR.Core.Infrastructure;

namespace Signify.eGFR.Core.EventHandlers.Akka;

public class KedLabResultHandler(
    ILogger<KedLabResultHandler> logger,
    IMessageSession messageSession,
    IApplicationTime applicationTime,
    IFeatureFlags featureFlags)
    : IHandleEvent<KedEgfrLabResult>
{
    /// <summary>
    /// Send NSB event to handle request
    /// </summary>
    /// <param name="event"></param>
    /// <param name="cancellationToken"></param>
    public async Task Handle(KedEgfrLabResult @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received LabResult with EvaluationId={EvaluationId}", @event.EvaluationId);
        
        if (!featureFlags.EnableKedLabResultIngestion)
        {
            logger.LogInformation("Ked Lab result ingestion is NOT enabled for EvaluationId={EvaluationId}", @event.EvaluationId);
            return;
        }
        @event.ReceivedByEgfrDateTime = applicationTime.UtcNow();
        await messageSession.SendLocal(@event, cancellationToken: cancellationToken);
        logger.LogInformation("End Handle LabResult {EvaluationId}", @event.EvaluationId);
    }
}