using Signify.Spirometry.Core.EventHandlers.Akka;

namespace Signify.Spirometry.Core.DI.Configs
{
    /// <summary>
    /// Configuration for consuming from Kafka.
    ///
    /// See: https://dev.azure.com/signifyhealth/HCC/_git/architecture.shared?path=/libraries/signify.akkastreams.kafka/src/Signify.AkkaStreams.Kafka/ConsumerOptions.cs
    /// </summary>
    internal class KafkaConsumerConfig : BaseKafkaConfig
    {
        /// <summary>
        /// Configuration key
        /// </summary>
        public const string Key = "KafkaConsumer";

        /// <summary>
        /// Kafka consumer group id
        /// </summary>
        public string GroupId { get; set; }

        /// <summary>
        /// The maximum number of offsets to commit in a batch to the broker
        /// </summary>
        public int CommitMaxBatchSize { get; set; }

        /// <summary>
        /// The minimum amount of time, in seconds, to wait between each backoff when failing to consume messages
        /// from the Akka stream
        /// </summary>
        public int MinimumBackoffSeconds { get; set; }
        /// <summary>
        /// The maximum amount of time, in seconds, to wait between each backoff when failing to consume messages
        /// from the Akka stream
        /// </summary>
        public int MaximumBackoffSeconds { get; set; }
        /// <summary>
        /// The maximum number of times to retry consuming a message when the consumer fails
        /// </summary>
        public int MaximumBackoffRetries { get; set; }
        /// <summary>
        /// The maximum amount of time, in seconds, to reach <see cref="MaximumBackoffRetries"/> when failing to consume
        /// messages from the Akka stream
        /// </summary>
        public int MaximumBackoffRetriesWithinSeconds { get; set; }

        /// <summary>
        /// Whether to continue streaming messages from the Akka stream if the consumer (ex <see cref="EvaluationFinalizedHandler"/>)
        /// failed to consume a message within the configured <see cref="MaximumBackoffRetries"/>.
        /// </summary>
        public bool ContinueOnFailure { get; set; }
        /// <summary>
        /// Whether to continue streaming messages from the Akka stream if there was an error attempting to deserialize
        /// the message.
        /// </summary>
        public bool ContinueOnDeserializationErrors { get; set; }
    }
}
