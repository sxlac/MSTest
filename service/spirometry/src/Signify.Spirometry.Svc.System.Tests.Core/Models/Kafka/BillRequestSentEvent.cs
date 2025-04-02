namespace Signify.Spirometry.Svc.System.Tests.Core.Models.Kafka;

public class BillRequestSentEvent
{
    public string BillingProductCode { get; set; }
    public string ProductCode { get; set; }
    public Guid BillId { get; set; }
    public int EvaluationId { get; set; }
    public int MemberPlanId { get; set; }
    public int ProviderId { get; set; }
    public DateTime PdfDeliveryDate { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime ReceivedDate { get; set; }
}