using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.Models;

public sealed class PerformedExamModel : ExamModel, IEquatable<PerformedExamModel>
{
    /// <summary>
    /// The raw results of this spirometry exam
    /// </summary>
    public RawExamResult ExamResult { get; }

    /// <summary>
    /// Creates a model based on evaluation where the provider performed a Spirometry exam
    /// </summary>
    /// <param name="evaluationId"></param>
    /// <param name="examResult">Raw results of the Spirometry exam</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="examResult"/> is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="evaluationId"/> is not positive</exception>
    public PerformedExamModel(int evaluationId, RawExamResult examResult)
        : base(evaluationId)
    {
        ExamResult = examResult ?? throw new ArgumentNullException(nameof(examResult));
    }

    #region IEquatable
    [ExcludeFromCodeCoverage]
    public bool Equals(PerformedExamModel other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return EqualityComparer.Equals(this, other) && Equals(ExamResult, other.ExamResult);
    }

    [ExcludeFromCodeCoverage]
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((PerformedExamModel) obj);
    }

    [ExcludeFromCodeCoverage]
    public override int GetHashCode()
    {
        return HashCode.Combine(EqualityComparer.GetHashCode(), ExamResult);
    }

    [ExcludeFromCodeCoverage]
    public static bool operator ==(PerformedExamModel left, PerformedExamModel right)
    {
        return Equals(left, right);
    }

    [ExcludeFromCodeCoverage]
    public static bool operator !=(PerformedExamModel left, PerformedExamModel right)
    {
        return !Equals(left, right);
    }
    #endregion IEquatable
}