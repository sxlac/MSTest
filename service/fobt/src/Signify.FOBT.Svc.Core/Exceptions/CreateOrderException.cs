using Refit;
using System;
using System.Net;

namespace Signify.FOBT.Svc.Core.Exceptions;

/// <summary>
/// Exception raised if there was an issue trying to create order
/// </summary>
public class CreateOrderException : Exception
{
    /// <summary>
    /// Corresponding evaluation's identifier
    /// </summary>
    public long EvaluationId { get; }

    /// <summary>
    /// HTTP status code received
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    public CreateOrderException(long evaluationId, HttpStatusCode statusCode, ApiException innerException = null)
        : base($"Order Creation failed for EvaluationId={evaluationId} with StatusCode={statusCode}", innerException)
    {
        EvaluationId = evaluationId;
        StatusCode = statusCode;
    }
}