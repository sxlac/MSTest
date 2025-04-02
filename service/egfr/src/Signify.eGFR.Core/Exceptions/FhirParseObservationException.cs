using System;
using System.Runtime.Serialization;

namespace Signify.eGFR.Core.Exceptions;

[Serializable]
public class FhirParseObservationException : FhirParseException
{
    public FhirParseObservationException(string message, long evaluationId) : base(message, evaluationId)
    {
    }

    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    protected FhirParseObservationException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}