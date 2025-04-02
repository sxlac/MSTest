using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.Models;

/// <summary>
/// Model for all Spirometry exams, whether actually performed or not
/// </summary>
public abstract class ExamModel
{
    protected static IEqualityComparer<ExamModel> EqualityComparer { get; } = new EvaluationIdEqualityComparer();

    /// <summary>
    /// Identifier of the evaluation that contained this Spirometry exam
    /// </summary>
    public int EvaluationId { get; }

    /// <summary>
    /// Version of the Form this evaluation corresponds to.
    /// </summary>
    public int FormVersionId { get; set; }

    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="evaluationId"/> is not positive</exception>
    protected ExamModel(int evaluationId)
    {
        if (evaluationId < 1)
            throw new ArgumentOutOfRangeException(nameof(evaluationId), evaluationId, "Must be positive");
        EvaluationId = evaluationId;
    }

    [ExcludeFromCodeCoverage]
    private sealed class EvaluationIdEqualityComparer : IEqualityComparer<ExamModel>
    {
        public bool Equals(ExamModel x, ExamModel y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.EvaluationId == y.EvaluationId && x.FormVersionId == y.FormVersionId;
        }

        public int GetHashCode(ExamModel obj)
        {
            return obj.EvaluationId;
        }
    }
}