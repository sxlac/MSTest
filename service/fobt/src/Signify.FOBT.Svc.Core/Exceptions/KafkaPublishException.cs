using System;

namespace Signify.FOBT.Svc.Core.Exceptions
{
    public class KafkaPublishException : Exception
    {
        public KafkaPublishException(string message) : base(message) { }
    }
}
