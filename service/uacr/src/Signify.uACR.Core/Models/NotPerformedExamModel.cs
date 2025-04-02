using System.Diagnostics.CodeAnalysis;
using System;

namespace Signify.uACR.Core.Models;

[ExcludeFromCodeCoverage]
public sealed class NotPerformedExamModel : ExamModel, IEquatable<NotPerformedExamModel>
{
    /// <summary>
    /// Details of why a uACR exam was not performed during this evaluation
    /// </summary>
    public NotPerformedInfo NotPerformedInfo { get; }

    /// <summary>
    /// Creates a model based on an evaluation where the provider did not perform a uACR exam
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="evaluationId"/> is not positive</exception>
    public NotPerformedExamModel(long evaluationId, NotPerformedInfo notPerformedInfo)
        : base(evaluationId)
    {
        NotPerformedInfo = notPerformedInfo ?? throw new ArgumentNullException(nameof(notPerformedInfo));
    }

    #region IEquatable
    public bool Equals(NotPerformedExamModel other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return EqualityComparer.Equals(this, other) && Equals(NotPerformedInfo, other.NotPerformedInfo);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((NotPerformedExamModel) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(EqualityComparer.GetHashCode(), NotPerformedInfo);
    }

    public static bool operator ==(NotPerformedExamModel left, NotPerformedExamModel right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(NotPerformedExamModel left, NotPerformedExamModel right)
    {
        return !Equals(left, right);
    }
    #endregion IEquatable
}