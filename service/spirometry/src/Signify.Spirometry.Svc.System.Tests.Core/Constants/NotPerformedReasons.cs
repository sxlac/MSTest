using Signify.Spirometry.Svc.System.Tests.Core.Models.Database;

namespace Signify.Spirometry.Svc.System.Tests.Core.Constants;

public class NotPerformedReasons
{
    public static readonly NotPerformedReasonModel TechnicalIssue = new(1, Answers.TechnicalIssueAnswerId, "Technical issue");
    public static readonly NotPerformedReasonModel EnvironmentalIssue = new(2, Answers.EnvironmentalIssueAnswerId, "Environmental issue");
    public static readonly NotPerformedReasonModel NoSuppliesOrEquipment = new(3, Answers.NoSuppliesOrEquipmentAnswerId, "No supplies or equipment");
    public static readonly NotPerformedReasonModel InsufficientTraining = new(4, Answers.InsufficientTrainingAnswerId, "Insufficient training");
    public static readonly NotPerformedReasonModel MemberPhysicallyUnable = new(8, Answers.MemberPhysicallyUnableAnswerId, "Member physically unable");
    public static readonly NotPerformedReasonModel MemberOutsideDemographicRanges = new(9, Answers.MemberOutsideDemographicRangesAnswerId, "Member outside demographic ranges");
    public static readonly NotPerformedReasonModel MemberRecentlyCompleted = new(4, Answers.MemberRecentlyCompletedAnswerId, "Member recently completed");
    public static readonly NotPerformedReasonModel ScheduledToComplete = new(5, Answers.MemberScheduledToCompleteAnswerId, "Scheduled to complete");
    public static readonly NotPerformedReasonModel MemberApprehension = new(6, Answers.MemberApprehensionAnswerId, "Member apprehension");
    public static readonly NotPerformedReasonModel NotInterested = new(7, Answers.MemberNotInterestedAnswerId, "Not interested");
}