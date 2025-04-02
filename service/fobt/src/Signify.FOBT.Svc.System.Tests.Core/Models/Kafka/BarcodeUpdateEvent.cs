namespace Signify.FOBT.Svc.System.Tests.Core.Models.Kafka;

public class BarcodeUpdateEvent
{
    public int EvaluationId { get; set; }
    public long MemberPlanId { get; set; }
    public string Barcode { get; set; }
    public string ProductCode { get; set; } 
    public string OrderCorrelationId { get; set; }
}