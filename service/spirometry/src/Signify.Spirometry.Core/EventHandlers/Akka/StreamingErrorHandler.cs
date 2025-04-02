using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka.Notifications;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.EventHandlers.Akka;

/// <summary>
/// Handler for handing <see cref="StreamingError"/> events raised by Akka, signaling the consumer
/// (ex <see cref="EvaluationFinalizedHandler"/>) failed to process the event within the configured
/// maximum backoff retries.
/// </summary>
[ExcludeFromCodeCoverage]
public class StreamingErrorHandler : IStreamingNotificationHandler<StreamingError>
{
    private readonly ILogger _logger;

    public StreamingErrorHandler(ILogger<StreamingErrorHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(StreamingError notification, CancellationToken cancellationToken)
    {
        _logger.LogError(notification.Exception,
            "Error streaming message on Topic={Topic} at Offset={Offset} on Partition={Partition}. DeserializedMessage: {Message}",
            notification.ConsumableMessage.Topic,
            notification.ConsumableMessage.CommittableOffset,
            notification.ConsumableMessage.Partition,
            notification.DeserializedObject);

        return Task.CompletedTask;
    }
}