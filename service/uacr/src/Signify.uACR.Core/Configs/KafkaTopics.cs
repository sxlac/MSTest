using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.uACR.Core.Configs;

[ExcludeFromCodeCoverage]
public class KafkaTopics
{
    public const string Key = "KafkaConsumer";
    
    /// <remarks>
    /// Key: Name/description of the topic
    /// Value: Topic
    /// </remarks>
    public Dictionary<string, string> Topics { get; set; }
}