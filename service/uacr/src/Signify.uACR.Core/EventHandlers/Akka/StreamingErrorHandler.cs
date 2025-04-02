using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka.Notifications;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using NewRelic.Api.Agent;

namespace Signify.uACR.Core.EventHandlers.Akka;

/// <summary>
/// Handler for handing <see cref="StreamingError"/> events raised by Akka, signaling the consumer
/// (ex <see cref="EvaluationFinalizedHandler"/>) failed to process the event within the configured
/// maximum backoff retries.
/// </summary>
[ExcludeFromCodeCoverage]
public class StreamingErrorHandler(ILogger<StreamingErrorHandler> logger)
    : IStreamingNotificationHandler<StreamingError>
{
    [Transaction]
    public Task Handle(StreamingError notification, CancellationToken cancellationToken)
    {
        logger.LogError(notification.Exception,
            "Error streaming message on Topic={Topic} at Offset={Offset} on Partition={Partition}. DeserializedMessage: {Message}",
            notification.ConsumableMessage.Topic,
            notification.ConsumableMessage.CommittableOffset,
            notification.ConsumableMessage.Partition,
            notification.DeserializedObject);

        return Task.CompletedTask;
    }
}