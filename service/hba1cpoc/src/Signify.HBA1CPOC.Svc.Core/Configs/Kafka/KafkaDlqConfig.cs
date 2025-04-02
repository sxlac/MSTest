using System.Diagnostics.CodeAnalysis;

namespace Signify.HBA1CPOC.Svc.Core.Configs.Kafka;

[ExcludeFromCodeCoverage]
public class KafkaDlqConfig
{
    public const string Key = "KafkaDlq";
    public bool IsDlqEnabled { get; set; }  
}