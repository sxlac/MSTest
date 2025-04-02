using Refit;
using System;
using System.Net;

namespace Signify.FOBT.Svc.Core.Exceptions;

/// <summary>
/// Exception raised if there was an issue sending a request to the ProviderPay API
/// </summary>
public class ProviderPayRequestException : Exception
{
    /// <summary>
    /// Identifier of the event that raised this exception
    /// </summary>
    public int FobtId { get; }
    /// <summary>
    /// Corresponding evaluation's identifier
    /// </summary>
    public long EvaluationId { get; }
    /// <summary>
    /// HTTP status code received from the ProviderPay API
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    public ProviderPayRequestException(int fobtId, long evaluationId, HttpStatusCode statusCode, string message, ApiException innerException = null)
        : base($"{message} for FOBTId={fobtId}, EvaluationId={evaluationId}, with StatusCode={statusCode}", innerException)
    {
        FobtId = fobtId;
        EvaluationId = evaluationId;
        StatusCode = statusCode;
    }
}