using Signify.DEE.Svc.System.Tests.Core.Models.Database;

namespace Signify.DEE.Svc.System.Tests.Core.Constants;

public static class NotPerformedReasons
{
    public static readonly NotPerformedReason MemberRecentlyCompleted = new(){ NotPerformedReasonId = 1, AnswerId = Answers.MemberRecentlyCompletedAnswerId, Reason = "Member recently completed"};
    public static readonly NotPerformedReason ScheduledToComplete = new(){ NotPerformedReasonId = 2, AnswerId = Answers.ScheduledToCompleteAnswerId, Reason = "Scheduled to complete"};
    public static readonly NotPerformedReason MemberApprehension = new(){ NotPerformedReasonId = 3, AnswerId = Answers.MemberApprehensionAnswerId, Reason = "Member apprehension"};
    public static readonly NotPerformedReason NotInterested = new(){ NotPerformedReasonId = 4, AnswerId = Answers.NotInterestedAnswerId, Reason = "Not interested"};
    public static readonly NotPerformedReason MemberRefusedOther = new(){ NotPerformedReasonId = 5, AnswerId = Answers.MemberRefusedOtherReasonAnswerId, Reason = "Other"};
    public static readonly NotPerformedReason TechnicalIssue = new(){ NotPerformedReasonId = 6, AnswerId = Answers.TechnicalIssueAnswerId, Reason = "Technical issue"};
    public static readonly NotPerformedReason EnvironmentalIssue = new(){ NotPerformedReasonId = 7, AnswerId = Answers.EnvironmentalIssueAnswerId, Reason = "Environmental issue"};
    public static readonly NotPerformedReason NoSuppliesOrEquipment = new(){ NotPerformedReasonId = 8, AnswerId = Answers.NoSuppliesOrEquipmentAnswerId, Reason = "No supplies or equipment"};
    public static readonly NotPerformedReason InsufficientTraining = new(){ NotPerformedReasonId = 9, AnswerId = Answers.InsufficientTrainingAnswerId, Reason = "Insufficient training"};
    public static readonly NotPerformedReason MemberPhysicallyUnable = new(){ NotPerformedReasonId = 10, AnswerId = Answers.MemberPhysicallyUnableAnswerId, Reason = "Member physically unable"};
    public static readonly NotPerformedReason UnableToPerformOther = new(){ NotPerformedReasonId = 11, AnswerId = Answers.UnableToPerformOtherReasonAnswerId, Reason = "Other"};
}