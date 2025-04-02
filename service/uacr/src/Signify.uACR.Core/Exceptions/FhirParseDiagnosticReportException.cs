using System;
using System.Runtime.Serialization;

namespace Signify.uACR.Core.Exceptions;

[Serializable]
public class FhirParseDiagnosticReportException : FhirParseException
{
    public FhirParseDiagnosticReportException(string message) : base(message)
    {
    }
    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    protected FhirParseDiagnosticReportException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}