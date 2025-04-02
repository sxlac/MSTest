using System;
using Refit;

namespace Signify.DEE.Svc.Core.Exceptions;

/// <summary>
/// Exception raised if there was an issue with ProviderPay
/// </summary>
[Serializable]
public class ProviderPayException(string evaluationId, Guid eventId, string message, ApiException innerException = null)
    : Exception($"{message}; EvaluationId={evaluationId}, EventId={eventId}", innerException)
{
    /// <summary>
    /// Identifier of the event that raised this exception
    /// </summary>
    public Guid EventId { get; } = eventId;

    /// <summary>
    /// Corresponding evaluation's identifier
    /// </summary>
    public string EvaluationId { get; } = evaluationId;
}