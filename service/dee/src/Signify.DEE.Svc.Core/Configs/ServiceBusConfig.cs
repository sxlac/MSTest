using System.Diagnostics.CodeAnalysis;

namespace Signify.DEE.Svc.Core.Configs;

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
        
    public int MessageProcessingConcurrencyLimit { get; set; }
}