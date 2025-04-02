using System;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace Signify.Spirometry.Core.Data.Entities;

/// <summary>
/// Details about a status change event for a Spirometry exam
/// </summary>
[ExcludeFromCodeCoverage]
#pragma warning disable S4035 // SonarQube - Classes implementing "IEquatable<T>" should be sealed - Cannot seal because EF extends this class (see virtual members)
public class ExamStatus : IEquatable<ExamStatus>
#pragma warning restore S4035
{
    /// <summary>
    /// Identifier of this exam status
    /// </summary>
    public int ExamStatusId { get; set; }
    /// <summary>
    /// Identifier of the Spirometry exam this status corresponds to
    /// </summary>
    public int SpirometryExamId { get; set; }
    /// <summary>
    /// Identifier of the status code for this exam status
    /// </summary>
    public int StatusCodeId { get; set; }
    /// <summary>
    /// The date and time when this status change event occurred
    /// </summary>
    public DateTime StatusDateTime { get; set; }
    /// <summary>
    /// The date and time when this notification was created within the Spirometry process manager
    /// </summary>
    public DateTime CreateDateTime { get; set; }

    /// <summary>
    /// The exam that this status update corresponds to
    /// </summary>
    public virtual SpirometryExam SpirometryExam { get; set; }

    public virtual StatusCode StatusCode { get; set; }

    #region Equality
    public bool Equals(ExamStatus other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return ExamStatusId == other.ExamStatusId
               && SpirometryExamId == other.SpirometryExamId
               && StatusCodeId == other.StatusCodeId
               && StatusDateTime.Equals(other.StatusDateTime)
               && CreateDateTime.Equals(other.CreateDateTime);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((ExamStatus) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ExamStatusId, SpirometryExamId, StatusCodeId, StatusDateTime, CreateDateTime);
    }

    public static bool operator ==(ExamStatus left, ExamStatus right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ExamStatus left, ExamStatus right)
    {
        return !Equals(left, right);
    }
    #endregion Equality
}