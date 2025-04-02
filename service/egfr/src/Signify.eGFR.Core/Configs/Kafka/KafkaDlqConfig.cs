namespace Signify.eGFR.Core.Configs.Kafka;

public class KafkaDlqConfig
{ 
    public const string Key = "KafkaDlq";

    public bool IsDlqEnabled { get; set; }
}