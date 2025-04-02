namespace Signify.PAD.Svc.System.Tests.Core.Models.Kafka;

public class PerformedEvent
{
    public int EvaluationId { get; set; }
    public long MemberPlanId { get; set; }
    public int ProviderId { get; set; }
    public DateTimeOffset CreateDate { get; set; }
    public DateTimeOffset ReceivedDate { get; set; }
    public string ProductCode { get; set; }
}