using System;

namespace Signify.HBA1CPOC.Svc.Core.Exceptions;

/// <summary>
/// Exception raised if there was no HBA1CPOC exam data is found in HBA1CPOC table
/// </summary>
[Serializable]
public class ExamNotFoundException(long evaluationId, Guid eventId)
    : Exception($"Unable to find an exam with EvaluationId={evaluationId}, for EventId={eventId}")
{
    public Guid EventId { get; } = eventId;

    public long EvaluationId { get; } = evaluationId;
}