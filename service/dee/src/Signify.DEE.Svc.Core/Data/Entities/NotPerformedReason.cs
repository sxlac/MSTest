using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.DEE.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class NotPerformedReason : IEqualityComparer<NotPerformedReason>
{
    public static readonly NotPerformedReason MemberRecentlyCompleted = new(1, 30943, "Member recently completed");
    public static readonly NotPerformedReason ScheduledToComplete = new(2, 30944, "Scheduled to complete");
    public static readonly NotPerformedReason MemberApprehension = new(3, 30945, "Member apprehension");
    public static readonly NotPerformedReason NotInterested = new(4, 30946, "Not interested");
    public static readonly NotPerformedReason Other = new(5, 30947, "Other");

    public static readonly NotPerformedReason Technical = new(6, 30950, "Technical issue");
    public static readonly NotPerformedReason EnvironmentalIssue = new(7, 30951, "Environmental issue");
    public static readonly NotPerformedReason NoSuppliesOrEquipment = new(8, 30952, "No supplies or equipment");
    public static readonly NotPerformedReason InsufficientTraining = new(9, 30953, "Insufficient training");
    public static readonly NotPerformedReason MemberPhysicallyUnable = new(10, 50914, "Member physically unable");


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