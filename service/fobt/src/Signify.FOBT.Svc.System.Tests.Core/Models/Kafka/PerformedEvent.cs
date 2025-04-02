namespace Signify.FOBT.Svc.System.Tests.Core.Models.Kafka;

public class PerformedEvent
{
    public int EvaluationId { get; set; }
    public long MemberPlanId { get; set; }
    public int ProviderId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ReceivedDate { get; set; }
    public string ProductCode { get; set; } 
    public string Barcode { get; set; } 
}