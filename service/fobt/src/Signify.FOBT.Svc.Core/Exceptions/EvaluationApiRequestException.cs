using System;
using System.Net;
using Refit;

namespace Signify.FOBT.Svc.Core.Exceptions;

/// <summary>
/// Exception raised if there was an issue sending a request to the Evaluation API
/// </summary>
public class EvaluationApiRequestException : Exception
{
    /// <summary>
    /// Corresponding evaluation's identifier
    /// </summary>
    public long EvaluationId { get; }

    /// <summary>
    /// HTTP status code received from the Evaluation API
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Custom error message to be logged
    /// </summary>
    public string ErrorMessage { get; }

    public EvaluationApiRequestException(long evaluationId, HttpStatusCode statusCode, string message, ApiException innerException = null)
        : base($"{message} for EvaluationId={evaluationId}, with StatusCode={statusCode}", innerException)
    {
        EvaluationId = evaluationId;
        StatusCode = statusCode;
        ErrorMessage = message;
    }
}