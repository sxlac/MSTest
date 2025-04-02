using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.HBA1CPOC.Svc.Core.Exceptions;

[ExcludeFromCodeCoverage]
public class KafkaPublishException:Exception
{
    public KafkaPublishException(string message = "") : base(message) { }
    public KafkaPublishException(Exception ex, string message = "") : base(message, ex) { }
}