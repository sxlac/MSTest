using System;
using Refit;

namespace Signify.uACR.Core.Exceptions;

/// <summary>
/// Exception raised if a bill request is sent and the Bill Id is null
/// </summary>
[Serializable]
public class RcmBillIdException(Guid eventId, long evaluationId, string message, ApiException innerException = null)
    : Exception($"{message} for EventId={eventId}, EvaluationId={evaluationId}", innerException)
{
    /// <summary>
    /// Identifier of the event that raised this exception
    /// </summary>
    public Guid EventId { get; } = eventId;

    /// <summary>
    /// Corresponding evaluation's identifier
    /// </summary>
    public long EvaluationId { get; } = evaluationId;
}