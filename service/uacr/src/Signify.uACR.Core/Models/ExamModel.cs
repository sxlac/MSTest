using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System;

namespace Signify.uACR.Core.Models;

/// <summary>
/// Model for all uACR exams, whether actually performed or not
/// </summary>
[ExcludeFromCodeCoverage]
public class ExamModel
{
    protected static IEqualityComparer<ExamModel> EqualityComparer { get; } = new EvaluationIdEqualityComparer();

    /// <summary>
    /// Identifier of the evaluation that contained this uACR exam
    /// </summary>
    public long EvaluationId { get; }
    
    /// <summary>
    /// The raw results of this uACR exam, if the exam was performed
    /// </summary>
    public RawExamResult ExamResult { get; }

    /// <summary>
    /// Version of the Form this evaluation corresponds to.
    /// </summary>
    public int FormVersionId { get; set; }
    
    /// <summary>
    /// If the uACR exam was not performed, this is the reason for it
    /// </summary>
    public NotPerformedReason? NotPerformedReason { get; }
    
    /// <summary>
    /// Gets or sets the Reason Notes for not performed
    /// </summary>
    public string Notes { get; set; }
    
    /// <summary>
    /// Whether or not a uACR exam was performed during this evaluation
    /// </summary>
    public bool ExamPerformed => !NotPerformedReason.HasValue;

    public ExamModel(long evaluationId)
    {
        if (evaluationId < 1)
            throw new ArgumentOutOfRangeException(nameof(evaluationId), evaluationId, "Must be positive");
        EvaluationId = evaluationId;
    }
    
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="evaluationId"/> is not positive</exception>
    public ExamModel(long evaluationId, NotPerformedReason notPerformedReason, string notes)
    {
        if (evaluationId < 1)
            throw new ArgumentOutOfRangeException(nameof(evaluationId), evaluationId, "Must be positive");
        EvaluationId = evaluationId;
        NotPerformedReason = notPerformedReason;
        Notes = notes;
    }
    
    public ExamModel(long evaluationId, RawExamResult examResult)
    {
        if (evaluationId < 1)
            throw new ArgumentOutOfRangeException(nameof(evaluationId), evaluationId, "Must be positive");
        EvaluationId = evaluationId;
        ExamResult = examResult ?? throw new ArgumentNullException(nameof(examResult));
    }

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
            return obj.EvaluationId.GetHashCode();
        }
    }
}