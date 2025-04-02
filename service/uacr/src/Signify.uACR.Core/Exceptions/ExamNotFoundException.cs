using System;

namespace Signify.uACR.Core.Exceptions;

[Serializable]
public class ExamNotFoundException(long evaluationId, Guid eventId)
    : Exception($"Unable to find an exam with EvaluationId={evaluationId}, for EventId={eventId}")
{
    public Guid EventId { get; } = eventId;

    public long EvaluationId { get; } = evaluationId;
}