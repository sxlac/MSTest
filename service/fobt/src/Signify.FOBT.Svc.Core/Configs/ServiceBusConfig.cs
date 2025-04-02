using System.Diagnostics.CodeAnalysis;
using Signify.FOBT.Svc.Core.Constants;

namespace Signify.FOBT.Svc.Core.Configs
{
    [ExcludeFromCodeCoverage]
    public class ServiceBusConfig
    {
        public string QueueName { get; set; }
        public string TopicName { get; set; }
        public int ImmediateRetryCount { get; set; }
        public int DelayedRetryCount { get; set; }
        public int DelayedRetrySecondsIncrease { get; set; }
        public int FinalTimeoutHours { get; set; }
        public int PersistenceCacheMinutes { get; set; }
        public bool UseOutbox { get; set; }
        public int OutboxDedupDays { get; set; }
        public int OutboxDedupCleanupMinutes { get; set; }
        public string TransportType { get; set; } = "AzureServiceBus";
        public string TransportConnection { get; set; } = ApplicationConstants.AzureServiceBus;
    }
}