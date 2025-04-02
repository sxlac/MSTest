using System;

namespace Signify.FOBT.Svc.Core.Exceptions;

/// <summary>
/// Exception raised if no FOBT exam data is found in FOBT table
/// </summary>
public class ExamNotFoundException : Exception
{
    public Guid EventId { get; }

    public long EvaluationId { get; }

    public ExamNotFoundException(long evaluationId, Guid eventId)
        : base($"Unable to find an exam with EvaluationId={evaluationId}, for EventId={eventId}")
    {
        EvaluationId = evaluationId;
        EventId = eventId;
    }
}