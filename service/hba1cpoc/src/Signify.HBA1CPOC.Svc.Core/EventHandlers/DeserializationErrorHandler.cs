using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka.Notifications;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Signify.AkkaStreams.Kafka;
using Signify.HBA1CPOC.Svc.Core.Configs.Kafka;
using Signify.HBA1CPOC.Svc.Core.Events;
using Signify.HBA1CPOC.Svc.Core.Infrastructure.Observability;

namespace Signify.HBA1CPOC.Svc.Core.EventHandlers;

/// <summary>
/// Handler for handling <see cref="DeserializationError"/> events raised by Akka, signaling it either failed
/// to locate a registered event type for the message (ex <see cref="EvaluationFinalizedEvent"/>), or that
/// the deserialization to the registered event type itself has failed.
/// </summary>
[ExcludeFromCodeCoverage]
public class DeserializationErrorHandler : IStreamingNotificationHandler<DeserializationError>
{
    private readonly ILogger _logger;
    private readonly IMessageProducer _messageProducer;
    private readonly KafkaDlqConfig _dlqConfig;
    private readonly IPublishObservability _publishObservability;

    public DeserializationErrorHandler(ILogger<DeserializationErrorHandler> logger, IMessageProducer messageProducer, KafkaDlqConfig dlqConfig, IPublishObservability publishObservability)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _messageProducer = messageProducer;
        _dlqConfig = dlqConfig;
        _publishObservability = publishObservability;
    }

    public Task Handle(DeserializationError error, CancellationToken cancellationToken)
    {
        _logger.LogError("Deserialization error={Error}", error);
        
        if (_dlqConfig.IsDlqEnabled)

        {
            var recordKey = error.Key ?? CreateRecordKey();
            _logger.LogInformation("Producing DlqMessage from topic: {TopicOfOrigin}, from partition:offset {Offset}:{Partition}, with key {Key}", 
                error.Topic, error.Offset, error.Partition, recordKey );
            var dlqMessage = GenerateMessageFromBase(new BaseDlqMessage
            {
                DeserializationError = error
            });
        
            _messageProducer.Produce(recordKey, dlqMessage, cancellationToken);
            
            var observabilityDlqEvent = new ObservabilityEvent
            {
                EventType = Constants.Observability.DeserializationErrors.ErrorPublishedToDlqEvent,
                EventValue = new Dictionary<string, object> {
                { Constants.Observability.DeserializationErrors.MessageKey, recordKey  }, 
                { Constants.Observability.DeserializationErrors.Topic, error.Topic },
                { Constants.Observability.DeserializationErrors.PublishingApplication, dlqMessage.PublishingApplication  },
                { Constants.Observability.DeserializationErrors.PublishTime, DateTimeOffset.Now } 
                }
            };

            _publishObservability.RegisterEvent(observabilityDlqEvent, true);
        }
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Method <c>CreateRecordKey</c> generates a GUID as the Kafka record key for the DLQ if the error message key is not present.
    /// </summary>
    private string CreateRecordKey()
    {
        var newRecordKey = Guid.NewGuid().ToString();
        _logger.LogInformation("Record key not present in DlqMessage, generating GUID as record key: {RecordKey}", newRecordKey);
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
            { "rcm_bill", () => new RcmBillDlqMessage() }
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