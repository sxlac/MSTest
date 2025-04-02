namespace Signify.uACR.Core.DI.Configs;

/// <summary>
/// Configuration for publishing to Kafka.
///
/// See: https://dev.azure.com/signifyhealth/HCC/_git/architecture.shared?path=/libraries/signify.akkastreams.kafka/src/Signify.AkkaStreams.Kafka/ProducerOptions.cs
/// </summary>
internal class KafkaProducerConfig : BaseKafkaConfig
{
    /// <summary>
    /// Configuration key
    /// </summary>
    public const string Key = "KafkaProducer";

    public int ProducerInstances { get; set; }

    #region Persistence settings
    public string PersistenceConnection { get; set; }

    public string PersistenceSchema { get; set; }

    public int PersistenceMaxRetries { get; set; }

    public int PollingInterval { get; set; }
    #endregion Persistence settings
}