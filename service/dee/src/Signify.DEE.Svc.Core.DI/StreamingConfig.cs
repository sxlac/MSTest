namespace Signify.DEE.Svc.Core.DI;

public class StreamingConfig
{
    public string LogLevel { get; set; }
    public int CommitMaxBatchSize { get; set; }
    public string KafkaBrokers { get; set; }
    public string SecurityProtocol { get; set; }
    public string Mechanism { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string KafkaGroupId { get; set; }
    public int MinimumBackoffSeconds { get; set; }
    public int MaximumBackoffSeconds { get; set; }
    public int MaximumBackoffRetries { get; set; }
    public int MaximumBackoffRetriesWithinSeconds { get; set; }
    public bool ContinueOnFailure { get; set; }
    public int SourceFailureRetryTimeoutMs { get; set; }
    public int SourceFailureMaxRetries { get; set; }
    public int ProducerInstance { get; set; }
    public int ProducerInstances { get; set; }
    public string PersistenceConnection { get; set; }
    public string PersistenceSchema { get; set; }
    public int PersistenceMaxRetries { get; set; }
    public int PollingInterval { get; set; }
    public bool ContinueOnDeserializationErrors { get; set; }
}