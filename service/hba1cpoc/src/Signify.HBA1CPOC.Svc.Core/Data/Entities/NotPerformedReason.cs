using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.HBA1CPOC.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class NotPerformedReason : IEqualityComparer<NotPerformedReason>
{
    public static readonly NotPerformedReason MemberRecentlyCompleted = new(1, 33074, "Member recently completed");
    public static readonly NotPerformedReason ScheduledToComplete = new(2, 33075, "Scheduled to complete");
    public static readonly NotPerformedReason MemberApprehension = new(3, 33076, "Member apprehension");
    public static readonly NotPerformedReason NotInterested = new(4, 33077, "Not interested");
    public static readonly NotPerformedReason Other = new(5, 33078, "Other");

    public static readonly NotPerformedReason Technical = new(6, 33081, "Technical issue");
    public static readonly NotPerformedReason EnvironmentalIssue = new(7, 33082, "Environmental issue");
    public static readonly NotPerformedReason NoSuppliesOrEquipment = new(8, 33083, "No supplies or equipment");
    public static readonly NotPerformedReason InsufficientTraining = new(9, 33084, "Insufficient training");
    public static readonly NotPerformedReason MemberPhysicallyUnable = new(10, 50905, "Member physically unable");


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
    public bool Equals(NotPerformedReason x, NotPerformedReason y)
    {
        if (ReferenceEquals(x, y)) return true;

        return x.NotPerformedReasonId == y.NotPerformedReasonId
               && x.AnswerId == y.AnswerId
               && x.Reason == y.Reason;
    }

    public int GetHashCode([DisallowNull] NotPerformedReason obj)
    {
        return HashCode.Combine(NotPerformedReasonId, AnswerId, Reason);
    }


    #endregion Equality
}