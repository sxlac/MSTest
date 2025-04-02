namespace Signify.eGFR.Core.Models;

/// <summary>
/// All possible reasons why an evaluation with a eGFR product may not have a completed eGFR exam
/// </summary>
public enum NotPerformedReason
{
    MemberRecentlyCompleted,
    ScheduledToComplete,
    MemberApprehension,
    NotInterested,
    TechnicalIssue,
    EnvironmentalIssue,
    NoSuppliesOrEquipment,
    InsufficientTraining,
    ClinicallyNotRelevant,
    MemberPhysicallyUnable
}