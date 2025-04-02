using System;

namespace Signify.eGFR.Core.Exceptions;

public class KafkaPublishException : Exception
{
    public KafkaPublishException(string message = "") : base(message) { }
    public KafkaPublishException(Exception ex, string message = "") : base(message, ex) { }
}