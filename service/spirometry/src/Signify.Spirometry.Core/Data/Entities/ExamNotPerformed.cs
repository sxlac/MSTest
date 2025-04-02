using System;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace Signify.Spirometry.Core.Data.Entities;

/// <summary>
/// Details explaining why a Spirometry exam that a provider was scheduled to perform was not
/// actually performed
/// </summary>
#pragma warning disable S4035 // SonarQube - Classes implementing "IEquatable<T>" should be sealed - Cannot seal because EF extends this class (see virtual members)
[ExcludeFromCodeCoverage]
public class ExamNotPerformed : IEquatable<ExamNotPerformed>
#pragma warning restore S4035
{
    /// <summary>
    /// Identifier of this record
    /// </summary>
    public int ExamNotPerformedId { get; set; }
    /// <summary>
    /// Foreign key identifier of the corresponding <see cref="SpirometryExam"/>
    /// </summary>
    public int SpirometryExamId { get; set; }
    /// <summary>
    /// Foreign key identifier of the corresponding <see cref="NotPerformedReason"/>
    /// </summary>
    public short NotPerformedReasonId { get; set; }
    /// <summary>
    /// Date and time this record was created
    /// </summary>
    public DateTime CreatedDateTime { get; set; }
    /// <summary>
    /// Optional free-text notes why the provider did not perform a spirometry exam
    /// </summary>
    public string Notes { get; set; }

    #region IEquality
    public bool Equals(ExamNotPerformed other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return ExamNotPerformedId == other.ExamNotPerformedId
               && SpirometryExamId == other.SpirometryExamId
               && NotPerformedReasonId == other.NotPerformedReasonId
               && CreatedDateTime.Equals(other.CreatedDateTime)
               && Notes == other.Notes;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ExamNotPerformed) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ExamNotPerformedId, SpirometryExamId, NotPerformedReasonId, CreatedDateTime, Notes);
    }

    public static bool operator ==(ExamNotPerformed left, ExamNotPerformed right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ExamNotPerformed left, ExamNotPerformed right)
    {
        return !Equals(left, right);
    }
    #endregion IEquality

    public virtual NotPerformedReason NotPerformedReason { get; set; }
    public virtual SpirometryExam SpirometryExam { get; set; }
}