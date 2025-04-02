using System;
using System.Runtime.Serialization;

namespace Signify.eGFR.Core.Exceptions;

[Serializable]
public class FhirParsePatientException : FhirParseException
{
    public FhirParsePatientException(string message) : base(message)
    {
    }

    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    protected FhirParsePatientException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}