using Signify.HBA1CPOC.System.Tests.Core.Models.Database;

namespace Signify.HBA1CPOC.System.Tests.Core.Constants;

public class NotPerformedReasons
{
    public static readonly NotPerformedReasonModel RecentlyCompleted = new(1, Answers.MemberRecentlyCompletedAnswerId, "Member recently completed");
    public static readonly NotPerformedReasonModel ScheduledToComplete = new(2, Answers.ScheduledToCompleteAnswerId, "Scheduled to complete");
    public static readonly NotPerformedReasonModel MemberApprehension = new(3, Answers.MemberApprehensionAnswerId, "Member apprehension");
    public static readonly NotPerformedReasonModel NotInterested = new(4, Answers.NotInterestedAnswerId, "Not interested");
    public static readonly NotPerformedReasonModel Other = new(5, Answers.OtherAnswerId, "Other");
    public static readonly NotPerformedReasonModel TechnicalIssue = new(6, Answers.TechnicalIssueAnswerId, "Technical issue");
    public static readonly NotPerformedReasonModel EnvironmentalIssue = new(7, Answers.EnvironmentalIssueAnswerId, "Environmental issue");
    public static readonly NotPerformedReasonModel NoSuppliesOrEquipment = new(8, Answers.NoSuppliesOrEquipmentAnswerId, "No supplies or equipment");
    public static readonly NotPerformedReasonModel InsufficientTraining = new(9, Answers.InsufficientTrainingAnswerId, "Insufficient training");
    public static readonly NotPerformedReasonModel MemberUnable = new(10, Answers.MemberUnableAnswerId, "Member physically unable");
}	