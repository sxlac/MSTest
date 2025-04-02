using System;

namespace Signify.DEE.Svc.Core.Exceptions;

/// <summary>
/// Exception raised if no DEE exam data is found in Exam table
/// </summary>
[Serializable]
public class ExamNotFoundException(long evaluationId, Guid eventId)
    : Exception($"Unable to find an exam with EvaluationId={evaluationId}, for EventId={eventId}")
{
    public Guid EventId { get; } = eventId;

    public long EvaluationId { get; } = evaluationId;
}