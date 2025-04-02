using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Signify.uACR.Core.Constants.Questions;

namespace Signify.uACR.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class NotPerformedReason
{
    public static readonly NotPerformedReason ScheduledToComplete = new(1, ReasonMemberRefusedQuestion.ScheduledToCompleteAnswerId, "Scheduled to complete");
    public static readonly NotPerformedReason MemberApprehension = new(2, ReasonMemberRefusedQuestion.MemberApprehensionAnswerId, "Member apprehension");
    public static readonly NotPerformedReason NotInterested = new(3, ReasonMemberRefusedQuestion.NotInterestedAnswerId, "Not interested");
    public static readonly NotPerformedReason TechnicalIssue = new(4, ReasonProviderUnableToPerformQuestion.TechnicalIssueAnswerId, "Technical issue");
    public static readonly NotPerformedReason EnvironmentalIssue = new(5, ReasonProviderUnableToPerformQuestion.EnvironmentalIssueAnswerId, "Environmental issue");
    public static readonly NotPerformedReason NoSuppliesOrEquipment = new(6, ReasonProviderUnableToPerformQuestion.NoSuppliesOrEquipmentAnswerId, "No supplies or equipment");
    public static readonly NotPerformedReason InsufficientTraining = new(7, ReasonProviderUnableToPerformQuestion.InsufficientTrainingAnswerId, "Insufficient training");
    public static readonly NotPerformedReason MemberPhysicallyUnable = new(8, ReasonProviderUnableToPerformQuestion.MemberPhysicallyUnableAnswerId, "Member physically unable");
    public static readonly NotPerformedReason MemberRecentlyCompleted = new(9, ReasonMemberRefusedQuestion.MemberRecentlyCompletedAnswerId, "Member recently completed");

    public short NotPerformedReasonId { get; set; }
    public int AnswerId { get; set; }
    public string Reason { get; set; }

    private NotPerformedReason(short notPerformedReasonId, int answerId, string reason)
    {
        NotPerformedReasonId = notPerformedReasonId;
        AnswerId = answerId;
        Reason = reason;
    }
        
    public virtual ICollection<ExamNotPerformed> ExamNotPerformeds { get; set; }
}