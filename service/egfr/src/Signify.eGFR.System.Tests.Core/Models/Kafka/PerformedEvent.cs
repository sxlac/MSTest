namespace Signify.eGFR.System.Tests.Core.Models.Kafka;

public class PerformedEvent
{
    public string Barcode { get; set; }
    public string ProductCode { get; set; }
    public int EvaluationId { get; set; }
    public long MemberPlanId { get; set; }
    public int ProviderId { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public DateTimeOffset ReceivedDate { get; set; }
    
}