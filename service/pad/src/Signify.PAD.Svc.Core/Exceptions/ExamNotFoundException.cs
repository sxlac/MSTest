using System;

namespace Signify.PAD.Svc.Core.Exceptions;

[Serializable]
public class ExamNotFoundException(long evaluationId, Guid eventId)
    : Exception($"Exam with EvaluationId={evaluationId} not found in DB. EventId={eventId}.")
{
    public Guid EventId { get; } = eventId;
    public long EvaluationId { get; } = evaluationId;
}