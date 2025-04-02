using System.Diagnostics.CodeAnalysis;
using Signify.AkkaStreams.Kafka.Notifications;

namespace Signify.FOBT.Svc.Core.Events.Akka.DLQ;

[ExcludeFromCodeCoverage]
public abstract class BaseDlqMessage
{
    public DeserializationError DeserializationError { get; set; }
    public readonly string PublishingApplication = Constants.ApplicationConstants.APP_ID;
}