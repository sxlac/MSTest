using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.eGFR.Core.Constants;
using Signify.eGFR.Core.Events;
using Signify.eGFR.Core.FeatureFlagging;
using Signify.eGFR.Core.Filters;

namespace Signify.eGFR.Core.EventHandlers.Akka;

public class InternalLabResultHandler(
    ILogger<InternalLabResultHandler> logger,
    IProductFilter productFilter,
    IMessageSession messageSession,
    IPublishObservability publishObservability,
    IFeatureFlags featureFlags)
    : IHandleEvent<LabResultReceivedEvent>
{
    /// <summary>
    /// Send NSB event to handle request
    /// </summary>
    /// <param name="event"></param>
    /// <param name="cancellationToken"></param>
    public async Task Handle(LabResultReceivedEvent @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received InternalLabResult with LabResultId={LabResultId}", @event.LabResultId);
        
        if (!featureFlags.EnableInternalLabResultIngestion)
        {
            logger.LogInformation("InternalLabResult ingestion is NOT enabled for LabResultId={LabResultId}", @event.LabResultId);
            return;
        }
        
        if (!productFilter.ShouldProcess(@event.ProductCodes))
        {
            logger.LogInformation("Product code not found, LabResult event ignored, for LabResultId={LabResultId}",
                @event.LabResultId);
            return;
        }
        
        await messageSession.SendLocal(@event, cancellationToken: cancellationToken);
        PublishObservability(@event, Observability.LabResult.InternalLabResultReceived);
        logger.LogInformation("End Handle InternalLabResult {LabResultId}", @event.LabResultId);
    }
    
    /// <summary>
    /// Publish new relic lab results event
    /// </summary>
    /// <param name="labResult">The lab results ingested</param>
    /// <param name="eventType">The event type to be published</param>
    private void PublishObservability(LabResultReceivedEvent labResult, string eventType)
    {
        var observabilityPerformedEvent = new ObservabilityEvent
        {
            EventType = eventType,
            EventValue = new Dictionary<string, object>
            {
                { Observability.EventParams.LabResultId, labResult.LabResultId },
                { Observability.EventParams.Vendor, labResult.VendorName },
                { Observability.EventParams.CreatedDateTime, labResult.ReceivedDateTime.ToUnixTimeSeconds() },
            }
        };

        publishObservability.RegisterEvent(observabilityPerformedEvent, true);
    }
}