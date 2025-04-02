using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka.Notifications;
using Signify.eGFR.Core.Events.Akka;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using NewRelic.Api.Agent;
using Signify.AkkaStreams.Kafka;
using Signify.Dps.Observability.Library.Services;
using Signify.eGFR.Core.Configs.Kafka;
using Signify.eGFR.Core.Events.Akka.DLQ;

namespace Signify.eGFR.Core.EventHandlers.Akka;

/// <summary>
/// Handler for handling <see cref="DeserializationError"/> events raised by Akka, signaling it either failed
/// to locate a registered event type for the message (ex <see cref="EvaluationFinalizedEvent"/>), or that
/// the deserialization to the registered event type itself has failed.
/// </summary>
[ExcludeFromCodeCoverage]
public class DeserializationErrorHandler(
    ILogger<DeserializationErrorHandler> logger, 
    IMessageProducer messageProducer, 
    KafkaDlqConfig dlqConfig, 
    IPublishObservability publishObservability)
    : IStreamingNotificationHandler<DeserializationError>
{
    [Transaction]
    public Task Handle(DeserializationError notification, CancellationToken cancellationToken)
    {
        // DeserializationError overrides ToString to include all pertinent details
        logger.LogError("Deserialization error: {DeserializationError}", notification);
        
        if (dlqConfig.IsDlqEnabled)
        {
            var recordKey = notification.Key == null ? CreateRecordKey() : notification.Key;
            logger.LogInformation("Producing DlqMessage from topic: {TopicOfOrigin}, from partition:offset {Offset}:{Partition}, with key {Key}", 
                notification.Topic, notification.Offset,notification.Partition, recordKey );
            var dlqMessage = GenerateMessageFromBase(new BaseDlqMessage
            {
                DeserializationError = notification
            });
        
            messageProducer.Produce(recordKey, dlqMessage, cancellationToken);
            publishObservability.Publish(
                Constants.Observability.DeserializationErrors.ErrorPublishedToDlqEvent, 
                new Dictionary<string, object> {
                    { Constants.Observability.DeserializationErrors.MessageKey, recordKey  },
                    { Constants.Observability.DeserializationErrors.Topic, notification.Topic },
                    { Constants.Observability.DeserializationErrors.PublishingApplication, dlqMessage.PublishingApplication  },
                    { Constants.Observability.DeserializationErrors.PublishTime, DateTimeOffset.Now }
                },true);
        }
        
        return Task.CompletedTask; 
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
    /// Method <c>GenerateMessageFromBase</c> creates the record type for the respective DLQ based on the topic that the
    /// poison message originated from.
    /// This method is not lovely, but this is how the AkkaStreams library works, routing is based
    /// on message type.
    /// </summary>
    private static BaseDlqMessage GenerateMessageFromBase(BaseDlqMessage baseDlqMessage)
    {
        var topicToDlqTypeMapping = new Dictionary<string, Func<BaseDlqMessage>>
        {
            { "evaluation", () => new EvaluationDlqMessage() },
            { "pdf_delivery", () => new PdfDeliveryDlqMessage() },
            { "cdi_events", () => new CdiEventDlqMessage() },
            { "rcm_bill", () => new RcmBillDlqMessage() },
            { "dps_labresult", () => new DpsLabResultDlqMessage() }
        };
        
#pragma warning disable CA1854
        if (!topicToDlqTypeMapping.ContainsKey(baseDlqMessage.DeserializationError.Topic))
            throw new InvalidOperationException();
#pragma warning restore CA1854
        
        var message = topicToDlqTypeMapping[baseDlqMessage.DeserializationError.Topic]();
        message.DeserializationError = baseDlqMessage.DeserializationError;
        return message;
    }
}
