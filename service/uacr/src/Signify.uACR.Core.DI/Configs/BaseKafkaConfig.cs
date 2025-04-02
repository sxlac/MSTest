using System.Collections.Generic;

namespace Signify.uACR.Core.DI.Configs;

/// <summary>
/// Shared Kafka configs between producer/consumer
/// </summary>
public class BaseKafkaConfig
{
    public string Brokers { get; set; }

    public string SecurityProtocol { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
        
    /// <remarks>
    /// Key: Name/description of the topic
    /// Value: Topic
    /// </remarks>
    public Dictionary<string, string> Topics { get; set; }
}