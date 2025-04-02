using Signify.uACR.Core.Constants;
using System.Diagnostics.CodeAnalysis;

namespace Signify.uACR.Core.Configs;

/// <summary>
/// Configs for NServiceBus
/// </summary>
[ExcludeFromCodeCoverage]
public class ServiceBusConfig
{
    /// <summary>
    /// Configuration section key for this config
    /// </summary>
    public const string Key = "ServiceBus";

    /// <summary>
    /// Type of transport to use
    /// </summary>
    public string TransportType { get; set; } = "AzureServiceBus";
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
    public string TransportConnection { get; set; } = ConnectionStringNames.AzureServiceBus;
}