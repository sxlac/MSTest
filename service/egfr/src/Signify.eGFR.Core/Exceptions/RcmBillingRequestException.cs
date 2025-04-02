using Refit;
using System;
using System.Net;

namespace Signify.eGFR.Core.Exceptions;

/// <summary>
/// Exception raised if there was an issue sending a billing request to the RCM API
/// </summary>
[Serializable]
public class RcmBillingRequestException(
    Guid eventId,
    long evaluationId,
    HttpStatusCode statusCode,
    string message,
    ApiException innerException = null)
    : Exception($"{message} for EventId={eventId}, EvaluationId={evaluationId}, with StatusCode={statusCode}",
        innerException)
{
    /// <summary>
    /// Identifier of the event that raised this exception
    /// </summary>
    public Guid EventId { get; } = eventId;

    /// <summary>
    /// Corresponding evaluation's identifier
    /// </summary>
    public long EvaluationId { get; } = evaluationId;

    /// <summary>
    /// HTTP status code received from the RCM API
    /// </summary>
    public HttpStatusCode StatusCode { get; } = statusCode;
}