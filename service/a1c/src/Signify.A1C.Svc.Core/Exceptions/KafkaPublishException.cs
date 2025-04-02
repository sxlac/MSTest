using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.A1C.Svc.Core.Exceptions
{
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class KafkaPublishException : Exception
    {
        public KafkaPublishException(string message = "") : base(message) { }
        public KafkaPublishException(Exception ex, string message = "") : base(message, ex) { }
    }
}