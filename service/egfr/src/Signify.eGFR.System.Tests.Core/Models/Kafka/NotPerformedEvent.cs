namespace Signify.eGFR.System.Tests.Core.Models.Kafka;

public class NotPerformedEvent
{
    public string ReasonType { get; set; }
    public string Reason { get; set; }
    public string ReasonNotes { get; set; }
    public string ProductCode { get; set; }
    public int EvaluationId { get; set; }
    public long MemberPlanId { get; set; }
    public int ProviderId { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public DateTimeOffset ReceivedDate { get; set; }
}   