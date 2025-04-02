using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka.Notifications;
using Signify.Spirometry.Core.Events.Akka;
using System.Threading;
using System.Threading.Tasks;
using NewRelic.Api.Agent;
using Signify.AkkaStreams.Kafka;
using Signify.Dps.Observability.Library.Services;
using Signify.Spirometry.Core.Events.Akka.DLQ;
using Signify.Spirometry.Core.FeatureFlagging;

namespace Signify.Spirometry.Core.EventHandlers.Akka;

/// <summary>
/// Handler for handling <see cref="DeserializationError"/> events raised by Akka, signaling it either failed
/// to locate a registered event type for the message (ex <see cref="EvaluationFinalizedEvent"/>), or that
/// the deserialization to the registered event type itself has failed.
/// </summary>
public class DeserializationErrorHandler(
    ILogger<DeserializationErrorHandler> logger, 
    IMessageProducer messageProducer, 
    IFeatureFlags featureFlags, 
    IPublishObservability publishObservability)
    : IStreamingNotificationHandler<DeserializationError>
{
    [Transaction]
    public async Task Handle(DeserializationError notification, CancellationToken cancellationToken)
    {
        // DeserializationError overrides ToString to include all pertinent details
        logger.LogError("Deserialization error: {DeserializationError}", notification);

        if (!featureFlags.EnableDlq)
            return;
        
        var recordKey = notification.Key == null ? CreateRecordKey() : notification.Key;
        logger.LogInformation("Producing DlqMessage from Topic: {TopicOfOrigin}, from Partition:Offset {Partition}:{Offset}, with Key {Key}", 
            notification.Topic, notification.Partition, notification.Offset, recordKey );
        var dlqMessage = GenerateDlqMessage(notification);
    
        await messageProducer.Produce(recordKey, dlqMessage, cancellationToken);
        publishObservability.Publish(
            Constants.Observability.DeserializationErrors.ErrorPublishedToDlqEvent, 
            new Dictionary<string, object> {
                { Constants.Observability.DeserializationErrors.MessageKey, recordKey  },
                { Constants.Observability.DeserializationErrors.Topic, notification.Topic },
                { Constants.Observability.DeserializationErrors.PublishingApplication, dlqMessage.PublishingApplication  },
                { Constants.Observability.DeserializationErrors.PublishTime, DateTimeOffset.Now }
            },true);
    }
    
    /// <summary>
    /// Method <c>CreateRecordKey</c> generates a GUID as the Kafka record key for the DLQ if the error message key is not present.
    /// </summary>
    private string CreateRecordKey()
    {
        var newRecordKey = Guid.NewGuid().ToString();
        logger.LogInformation("Record key not present in DlqMessage, generating GUID as record key: {RecordKey}", newRecordKey);
        return newRecordKey;
    }
    
    /// <summary>
    /// Method <c>GenerateDlqMessage</c> creates the record type for the respective DLQ based on the topic that the
    /// poison message originated from.
    /// This method is not lovely, but this is how the AkkaStreams library works, routing is based
    /// on message type.
    /// </summary>
    private static BaseDlqMessage GenerateDlqMessage(DeserializationError notification)
    {
        var topicToDlqTypeMapping = new Dictionary<string, Func<BaseDlqMessage>>
        {
            { "evaluation", () => new EvaluationDlqMessage() },
            { "pdfdelivery", () => new PdfDeliveryDlqMessage() },
            { "overread_spirometry", () => new OverreadDlqMessage() },
            { "cdi_holds", () => new CdiHoldsDlqMessage() },
            { "cdi_events", () => new CdiEventDlqMessage() },
            { "rcm_bill", () => new RcmBillDlqMessage() }
        };

        if (!topicToDlqTypeMapping.TryGetValue(notification.Topic, out Func<BaseDlqMessage> value))
            throw new InvalidOperationException($"An error occurred while attempting to generate a DLQ message. The topic ({notification.Topic}) is not recognized.");

        var message = value(); 
        message.DeserializationError = notification;
        return message;
    }
}