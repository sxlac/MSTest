namespace Signify.eGFR.System.Tests.Core.Models.Kafka;

public class BillRequestNotSentEvent
{
    public string BillingProductCode { get; set; }
    public string ProductCode { get; set; }
    public int EvaluationId { get; set; }
    public int MemberPlanId { get; set; }
    public int ProviderId { get; set; }
    public DateTime PdfDeliveryDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ReceivedDate { get; set; }
}