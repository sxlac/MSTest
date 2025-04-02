using System;

namespace Signify.eGFR.Core.Models;

public abstract class BusinessRuleAnswers(long evaluationId, Guid eventId)
{
    public string NormalityCode { get; set; }
    public long EvaluationId { get; init; } = evaluationId;
    public Guid EventId { get; init; } = eventId;
}