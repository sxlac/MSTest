namespace Signify.uACR.Core.Models;

/// <summary>
/// All possible reasons why an evaluation with a uACR product may not have a completed uACR exam
/// </summary>
public enum NotPerformedReason
{
    ScheduledToComplete,
    MemberApprehension,
    NotInterested,
    TechnicalIssue,
    EnvironmentalIssue,
    NoSuppliesOrEquipment,
    InsufficientTraining,
    MemberPhysicallyUnable,
    MemberRecentlyCompleted
}