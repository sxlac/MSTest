using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.FOBT.Svc.Core.Data.Entities;

/// <summary>
/// A reason why a provider may not have performed a Spirometry exam during
/// an evaluation that included a Spirometry product
/// </summary>
[ExcludeFromCodeCoverage]
public class NotPerformedReason : IEquatable<NotPerformedReason>
{
    public static readonly NotPerformedReason MemberRecentlyCompleted = new NotPerformedReason(1, 30879, "Member recently completed");
    public static readonly NotPerformedReason ScheduledToComplete = new NotPerformedReason(2, 30880, "Scheduled to complete");
    public static readonly NotPerformedReason MemberApprehension = new NotPerformedReason(3, 30881, "Member apprehension");
    public static readonly NotPerformedReason NotInterested = new NotPerformedReason(4, 30882, "Not interested");
    public static readonly NotPerformedReason Other = new NotPerformedReason(5, 30883, "Other");
    public static readonly NotPerformedReason Technical = new NotPerformedReason(6, 30886, "Technical issue");
    public static readonly NotPerformedReason EnvironmentalIssue = new NotPerformedReason(7, 30887, "Environmental issue");
    public static readonly NotPerformedReason NoSuppliesOrEquipment = new NotPerformedReason(8, 30888, "No supplies or equipment");
    public static readonly NotPerformedReason InsufficientTraining = new NotPerformedReason(9, 30889, "Insufficient training");
    public static readonly NotPerformedReason MemberPhysicallyUnable = new NotPerformedReason(10, 50908, "Member physically unable");

    /// <summary>
    /// Identifier of this reason
    /// </summary>
    public short NotPerformedReasonId { get; set; }
        
    /// <summary>
    /// Identifier of the corresponding evaluation answer
    /// </summary>
    public int AnswerId { get; set; }
        
    /// <summary>
    /// Descriptive reason for why the exam was not performed
    /// </summary>
    public string Reason { get; set; }

    private NotPerformedReason(short notPerformedReasonId, int answerId, string reason)
    {
        NotPerformedReasonId = notPerformedReasonId;
        AnswerId = answerId;
        Reason = reason;
    }

    public NotPerformedReason()
    {
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
        return Equals((NotPerformedReason)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(NotPerformedReasonId, AnswerId, Reason);
    }     
    #endregion Equality
}