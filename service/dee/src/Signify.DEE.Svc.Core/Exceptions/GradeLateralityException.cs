using System;

namespace Signify.DEE.Svc.Core.Exceptions;

/// <summary>
/// Exception raised when while submitting grade for laterality, laterality has a unexpected value.
/// </summary>
[Serializable]
public class GradeLateralityException(long evaluationId, string message) : Exception(message)
{
    /// <summary>
    /// Corresponding evaluation's identifier
    /// </summary>
    public long EvaluationId { get; } = evaluationId;

    public GradeLateralityException(long evaluationId)
        : this(evaluationId, $"Invalid Laterality Code received while submitting Laterality Grade, for EvaluationId={evaluationId}")
    {
    }
}