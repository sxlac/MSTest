using System;

namespace Signify.eGFR.Core.Models;

/// <summary>
/// Model for all eGFR exams, whether actually performed or not
/// </summary>
public sealed class ExamModel : IEquatable<ExamModel>
{
    /// <summary>
    /// Identifier of the evaluation that contained this eGFR exam
    /// </summary>
    public long EvaluationId { get; }

    /// <summary>
    /// The raw results of this eGFR exam, if the exam was performed
    /// </summary>
    public RawExamResult ExamResult { get; }

    /// <summary>
    /// If the eGFR exam was not performed, this is the reason for it
    /// </summary>
    public NotPerformedReason? NotPerformedReason { get; }

    /// <summary>
    /// Gets or sets the Reason Notes for not performed
    /// </summary>
    public string Notes { get; set; }
    
    /// <summary>
    /// Whether or not a eGFR exam was performed during this evaluation
    /// </summary>
    public bool ExamPerformed => !NotPerformedReason.HasValue;

    public ExamModel(long evaluationId)
    {
        if (evaluationId < 1)
            throw new ArgumentOutOfRangeException(nameof(evaluationId), evaluationId, "Must be positive");
        EvaluationId = evaluationId;
    }

    /// <summary>
    /// Creates a model based on evaluation where the provider performed a eGFR exam
    /// </summary>
    /// <param name="evaluationId"></param>
    /// <param name="examResult">Raw results of the eGFR exam</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="examResult"/> is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="evaluationId"/> is not positive</exception>
    public ExamModel(long evaluationId, RawExamResult examResult)
        : this(evaluationId)
    {
        ExamResult = examResult ?? throw new ArgumentNullException(nameof(examResult));
    }

    /// <summary>
    /// Creates a model based on an evaluation where the provider did not perform a eGFR exam
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="evaluationId"/> is not positive</exception>
    public ExamModel(long evaluationId, NotPerformedReason notPerformedReason, string notes)
        : this(evaluationId)
    {
        NotPerformedReason = notPerformedReason;
        Notes = notes;
    }

    #region IEquatable
    public bool Equals(ExamModel other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return EvaluationId == other.EvaluationId
               && NotPerformedReason == other.NotPerformedReason;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((ExamModel) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(EvaluationId, ExamResult, NotPerformedReason);
    }

    public static bool operator ==(ExamModel left, ExamModel right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ExamModel left, ExamModel right)
    {
        return !Equals(left, right);
    }
    #endregion IEquatable
}