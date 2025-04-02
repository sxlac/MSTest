using System;
using System.Runtime.Serialization;

namespace Signify.eGFR.Core.Exceptions;

[Serializable]
public class FhirParseException : Exception
{
    public long? EvaluationId { get; set; }
    public string VendorName { get; set; }

    public FhirParseException(string errorDescription,
        long? evaluationId,
        string vendorName,
        long? labResultId,
        Exception innerException)
        : base(
            $"{errorDescription} EvaluationId: {evaluationId}, Vendor Name: {vendorName}, LabResultId: {labResultId}",
            innerException)
    {
        EvaluationId = evaluationId;
        VendorName = vendorName;
    }

    public FhirParseException(
        string errorDescription,
        string vendorName,
        long? labResultId,
        Exception innerException)
        : base(
            $"{errorDescription} Vendor Name: {vendorName}, LabResultId: {labResultId}",
            innerException)
    {
        VendorName = vendorName;
    }

    public FhirParseException(string errorMessage) : base($"{errorMessage}")
    {
    }

    public FhirParseException(string message, long? evaluationId) : base($"{message} EvaluationId: {evaluationId}")
    {
        EvaluationId = evaluationId;
    }

    [Obsolete(
        "This API supports obsolete formatter-based serialization. It should not be called or extended by application code.",
        DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    protected FhirParseException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}