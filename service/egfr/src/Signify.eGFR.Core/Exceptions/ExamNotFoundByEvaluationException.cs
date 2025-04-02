using System;

namespace Signify.eGFR.Core.Exceptions;

[Serializable]
public class ExamNotFoundByEvaluationException(long evaluationId, Guid eventId)
    : Exception($"Unable to find an exam with EvaluationId={evaluationId}, for EventId={eventId}")
{
    public Guid EventId { get; } = eventId;

    public long EvaluationId { get; } = evaluationId;
}