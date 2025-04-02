using System.Diagnostics.CodeAnalysis;
using Signify.AkkaStreams.Kafka.Notifications;

namespace Signify.uACR.Core.Events.Akka.DLQ;

[ExcludeFromCodeCoverage]
public abstract class BaseDlqMessage
{
    public DeserializationError DeserializationError { get; set; }
    public readonly string PublishingApplication = Constants.Application.ApplicationId;
}
