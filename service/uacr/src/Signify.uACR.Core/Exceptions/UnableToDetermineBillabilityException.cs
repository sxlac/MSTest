using System;

namespace Signify.uACR.Core.Exceptions;

[Serializable]
public class UnableToDetermineBillabilityException(Guid eventId, long evaluationId) : Exception(
    $"Insufficient information known about evaluation to determine billability, for EventId={eventId}, EvaluationId={evaluationId}")
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