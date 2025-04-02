using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.AkkaStreams.Kafka.Notifications;

namespace Signify.HBA1CPOC.Svc.Core.NotificationHandlers;

/// <summary>
/// Handler for handling <see cref="DeserializationError"/> events raised by Akka, signaling it either failed
/// to locate a registered event type for the message (ex <see cref="EvaluationFinalizedEvent"/>), or that
/// the deserialization to the registered event type itself has failed.
/// </summary>
[ExcludeFromCodeCoverage]
public class DeserializationErrorHandler : IStreamingNotificationHandler<DeserializationError>
{
    private readonly ILogger _logger;

    public DeserializationErrorHandler(ILogger<DeserializationErrorHandler> logger)
    {
        _logger = logger;
    }

    [Transaction]
    public Task Handle(DeserializationError notification, CancellationToken cancellationToken)
    {
        // DeserializationError overrides ToString to include all pertinent details
        _logger.LogError("Deserialization error: {DeserializationError}", notification);

        return Task.CompletedTask;
    }
}