namespace Signify.PAD.Svc.System.Tests.Core.Models.Kafka;

public class ProviderPayRequestSent
{
    public string PaymentId { get; set; }
    public string ProviderPayProductCode { get; set; }
    public string ProductCode { get; set; }
    public int EvaluationId { get; set; }
    public int ProviderId { get; set; }
    public long MemberPlanId { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime ReceivedDate { get; set; }
    
}