namespace Signify.Spirometry.Core.Models
{
    /// <summary>
    /// All possible reasons why an evaluation with a Spirometry product may not have a completed Spirometry exam
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
        MemberPhysicallyUnable,
        MemberOutsideDemographicRanges
    }
}
