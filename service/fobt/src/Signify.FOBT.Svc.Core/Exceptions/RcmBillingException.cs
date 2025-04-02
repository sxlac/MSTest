using Refit;
using System;
using System.Net;

namespace Signify.FOBT.Svc.Core.Exceptions;

/// <summary>
/// Exception raised if there was an issue sending a request to the ProviderPay API
/// </summary>
public class RcmBillingException : Exception
{
    /// <summary>
    /// Corresponding evaluation's identifier
    /// </summary>
    public long EvaluationId { get; }

    /// <summary>
    /// HTTP status code received from the ProviderPay API
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    public RcmBillingException(long evaluationId, HttpStatusCode statusCode, string message, ApiException innerException = null)
        : base($"{message} for EvaluationId={evaluationId}, with StatusCode={statusCode}", innerException)
    {
        EvaluationId = evaluationId;
        StatusCode = statusCode;
    }
}