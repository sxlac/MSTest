using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace Signify.Spirometry.Core.Data.Entities;

/// <summary>
/// A reason why a provider may not have performed a Spirometry exam during
/// an evaluation that included a Spirometry product
/// </summary>
#pragma warning disable S4035 // SonarQube - Classes implementing "IEquatable<T>" should be sealed - Cannot seal because EF extends this class (see virtual members)
[ExcludeFromCodeCoverage]
public class NotPerformedReason : IEquatable<NotPerformedReason>
#pragma warning restore S4035
{
    public static readonly NotPerformedReason MemberRecentlyCompleted = new NotPerformedReason(1, 50923, "Member recently completed");
    public static readonly NotPerformedReason ScheduledToComplete = new NotPerformedReason(2, 50924, "Scheduled to complete");
    public static readonly NotPerformedReason MemberApprehension = new NotPerformedReason(3, 50925, "Member apprehension");
    public static readonly NotPerformedReason NotInterested = new NotPerformedReason(4, 50926, "Not interested");
    public static readonly NotPerformedReason TechnicalIssue = new NotPerformedReason(5, 50928, "Technical issue");
    public static readonly NotPerformedReason EnvironmentalIssue = new NotPerformedReason(6, 50929, "Environmental issue");
    public static readonly NotPerformedReason NoSuppliesOrEquipment = new NotPerformedReason(7, 50930, "No supplies or equipment");
    public static readonly NotPerformedReason InsufficientTraining = new NotPerformedReason(8, 50931, "Insufficient training");
    public static readonly NotPerformedReason MemberPhysicallyUnable = new NotPerformedReason(9, 50932, "Member physically unable");
    public static readonly NotPerformedReason MemberOutsideDemographicRanges = new NotPerformedReason(10, 51960, "Member outside demographic ranges");
        
    /// <summary>
    /// Identifier of this reason
    /// </summary>
    public short NotPerformedReasonId { get; init; }
    /// <summary>
    /// Identifier of the corresponding evaluation answer
    /// </summary>
    public int AnswerId { get; init; }
    /// <summary>
    /// Descriptive reason for why the exam was not performed
    /// </summary>
    public string Reason { get; init; }

    public NotPerformedReason(short notPerformedReasonId, int answerId, string reason)
    {
        NotPerformedReasonId = notPerformedReasonId;
        AnswerId = answerId;
        Reason = reason;
    }

    #region Equality
    public bool Equals(NotPerformedReason other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return NotPerformedReasonId == other.NotPerformedReasonId
               && AnswerId == other.AnswerId
               && Reason == other.Reason;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((NotPerformedReason) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(NotPerformedReasonId, AnswerId, Reason);
    }

    public static bool operator ==(NotPerformedReason left, NotPerformedReason right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(NotPerformedReason left, NotPerformedReason right)
    {
        return !Equals(left, right);
    }
    #endregion Equality

    public virtual ICollection<ExamNotPerformed> ExamNotPerformeds { get; set; } = new HashSet<ExamNotPerformed>();
}