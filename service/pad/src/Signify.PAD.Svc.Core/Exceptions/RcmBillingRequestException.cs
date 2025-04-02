using System;
using System.Net;
using Refit;

namespace Signify.PAD.Svc.Core.Exceptions;
    
/// <summary>
/// Exception raised if there was an issue sending a billing request to the RCM API
/// </summary>
[Serializable]
public class RcmBillingRequestException(
    long evaluationId,
    HttpStatusCode statusCode,
    string message,
    ApiException innerException = null)
    : Exception($"{message} for EvaluationId={evaluationId}, with StatusCode={statusCode}", innerException)
{
    /// <summary>
    /// Corresponding evaluation's identifier
    /// </summary>
    public long EvaluationId { get; } = evaluationId;

    /// <summary>
    /// HTTP status code received from the RCM API
    /// </summary>
    public HttpStatusCode StatusCode { get; } = statusCode;
}