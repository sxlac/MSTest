using Signify.uACR.Core.Data.Entities;
using System.Diagnostics.CodeAnalysis;
using System;

namespace Signify.uACR.Core.Models;

[ExcludeFromCodeCoverage]
public abstract class BusinessRuleAnswers(long evaluationId, Guid eventId)
{
    public long EvaluationId { get; init; } = evaluationId;
    public Guid EventId { get; init; } = eventId;
    public LabResult Result { get; set; }
}