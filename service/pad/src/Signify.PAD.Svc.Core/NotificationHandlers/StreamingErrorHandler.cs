using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.AkkaStreams.Kafka.Notifications;
using Signify.PAD.Svc.Core.EventHandlers;

namespace Signify.PAD.Svc.Core.NotificationHandlers;


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

    [Transaction]
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