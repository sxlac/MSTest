using Signify.AkkaStreams.Kafka.Notifications;

namespace Signify.eGFR.Core.Events.Akka.DLQ;


public class BaseDlqMessage
{
    public DeserializationError DeserializationError { get; set; }
    public readonly string PublishingApplication = Constants.Application.ApplicationId;
}
