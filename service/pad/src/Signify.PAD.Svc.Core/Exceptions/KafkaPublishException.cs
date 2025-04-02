using System;

namespace Signify.PAD.Svc.Core.Exceptions;

[Serializable]
public class KafkaPublishException(string message) : Exception(message);