using System;

namespace Signify.uACR.Core.Exceptions;

[Serializable]
public class KafkaPublishException(string message) : Exception(message);