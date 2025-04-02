using System;

namespace Signify.HBA1CPOC.Svc.Core.Exceptions;

[Serializable]
public class ExamWithStatusNotFoundException(long evaluationId, Guid eventId, string status)
    : Exception($"Exam with EvaluationId={evaluationId} and Status={status} not found in DB. EventId={eventId}.")
{
    public long EvaluationId { get; } = evaluationId;
    public Guid EventId { get; } = eventId;
    public string Status { get; } = status;
}