using System;
using System.Net;
using Refit;

namespace Signify.HBA1CPOC.Svc.Core.Exceptions;

/// <summary>
/// Exception raised if there was an issue sending a request to the ProviderPay API
/// </summary>
[Serializable]
public class ProviderPayRequestException(
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
    /// HTTP status code received from the ProviderPay API
    /// </summary>
    public HttpStatusCode StatusCode { get; } = statusCode;
}