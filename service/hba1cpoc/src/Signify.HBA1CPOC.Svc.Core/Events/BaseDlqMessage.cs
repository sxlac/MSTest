using System.Diagnostics.CodeAnalysis;
using Signify.AkkaStreams.Kafka.Notifications;

namespace Signify.HBA1CPOC.Svc.Core.Events;

[ExcludeFromCodeCoverage]
public class BaseDlqMessage
{
    public DeserializationError DeserializationError { get; set; }
    public string PublishingApplication = "Signify.Dps.HBA1CPOC";
}
