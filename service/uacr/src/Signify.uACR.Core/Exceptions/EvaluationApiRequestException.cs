using System;
using System.Net;
using Refit;

namespace Signify.uACR.Core.Exceptions;

/// <summary>
/// Exception raised if there was an issue sending a request to the Evaluation API
/// </summary>
[Serializable]
public class EvaluationApiRequestException(
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
    /// HTTP status code received from the Evaluation API
    /// </summary>
    public HttpStatusCode StatusCode { get; } = statusCode;

    /// <summary>
    /// Custom error message to be logged
    /// </summary>
    public string ErrorMessage { get; } = message;
}