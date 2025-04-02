using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Signify.Spirometry.Core.Exceptions;

[Serializable]
public sealed class KafkaPublishException : Exception
{
    public KafkaPublishException(string message)
        : base(message) { }

    [ExcludeFromCodeCoverage]
    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    private KafkaPublishException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}