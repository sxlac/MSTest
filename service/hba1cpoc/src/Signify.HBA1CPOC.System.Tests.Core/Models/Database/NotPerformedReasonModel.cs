namespace Signify.HBA1CPOC.System.Tests.Core.Models.Database;

public class NotPerformedReasonModel
{
    public int NotPerformedReasonId { get; set; }
    public int AnswerId { get; set; }
    public string Reason { get; set; }
    
    public NotPerformedReasonModel(int notPerformedReasonId, int answerId, string reason)
    {
        NotPerformedReasonId = notPerformedReasonId;
        AnswerId = answerId;
        Reason = reason;
    }
}