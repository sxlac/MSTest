namespace Signify.Spirometry.Core.DI.Configs
{
    /// <summary>
    /// Configs for streaming Akka
    /// </summary>
    internal class KafkaStreamingConfig
    {
        /// <summary>
        /// Configuration key
        /// </summary>
        public const string Key = "KafkaStreaming";

        /// <summary>
        /// Akka event logging level
        /// </summary>
        /// <remarks>Supported values: "DEBUG", "INFO", "WARNING", "ERROR"</remarks>
        public string LogLevel { get; set; } = "INFO";
    }
}