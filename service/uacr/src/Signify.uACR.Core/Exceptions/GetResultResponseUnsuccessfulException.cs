using System;
using System.Net;

namespace Signify.uACR.Core.Exceptions;

public class GetResultResponseUnsuccessfulException(
    long labResultId,
    string vendor,
    string testName,
    HttpStatusCode httpStatusCode,
    Exception innerException = null)
    : Exception(
        $"Unsuccessful Http Response for LabResultId Id:{labResultId}, Vendor:{vendor}, Test:{testName}, HttpStatusCode:{httpStatusCode}",
        innerException)
{
    public long LabResultId { get; } = labResultId;
    public string TestName { get; } = testName;
    public string Vendor { get; } = vendor;
    public HttpStatusCode HttpStatusCode { get; } = httpStatusCode;
}