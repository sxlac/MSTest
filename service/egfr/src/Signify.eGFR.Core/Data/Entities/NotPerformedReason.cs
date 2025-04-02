using Signify.eGFR.Core.Constants.Questions;
using System.Collections.Generic;

namespace Signify.eGFR.Core.Data.Entities;

/// <summary>
/// A reason why a provider may not have performed a eGFR exam during
/// an evaluation that included a eGFR product
/// </summary>
public class NotPerformedReason
{
    public static readonly NotPerformedReason MemberRecentlyCompleted = new(1, ReasonMemberRefusedQuestion.MemberRecentlyCompletedAnswerId, "Member recently completed");
    public static readonly NotPerformedReason ScheduledToComplete = new(2, ReasonMemberRefusedQuestion.ScheduledToCompleteAnswerId, "Scheduled to complete");
    public static readonly NotPerformedReason MemberApprehension = new(3, ReasonMemberRefusedQuestion.MemberApprehensionAnswerId, "Member apprehension");
    public static readonly NotPerformedReason NotInterested = new(4, ReasonMemberRefusedQuestion.NotInterestedAnswerId, "Not interested");
    public static readonly NotPerformedReason TechnicalIssue = new(5, ReasonProviderUnableToPerformQuestion.TechnicalIssueAnswerId, "Technical issue");
    public static readonly NotPerformedReason EnvironmentalIssue = new(6, ReasonProviderUnableToPerformQuestion.EnvironmentalIssueAnswerId, "Environmental issue");
    public static readonly NotPerformedReason NoSuppliesOrEquipment = new(7, ReasonProviderUnableToPerformQuestion.NoSuppliesOrEquipmentAnswerId, "No supplies or equipment");
    public static readonly NotPerformedReason InsufficientTraining = new(8, ReasonProviderUnableToPerformQuestion.InsufficientTrainingAnswerId, "Insufficient training");
    public static readonly NotPerformedReason ClinicallyNotRelevant = new(9, ReasonNotPerformedQuestion.ClinicallyNotRelevant, "Clinically not relevant");
    public static readonly NotPerformedReason MemberPhysicallyUnable = new(10, ReasonProviderUnableToPerformQuestion.MemberPhysicallyUnableAnswerId, "Member physically unable");
        
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

    public virtual ICollection<ExamNotPerformed> ExamsNotPerformed { get; set; } = new HashSet<ExamNotPerformed>();
}