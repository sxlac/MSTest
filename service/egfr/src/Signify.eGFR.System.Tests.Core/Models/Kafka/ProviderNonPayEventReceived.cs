namespace Signify.eGFR.System.Tests.Core.Models.Kafka;

public class ProviderNonPayEventReceived
{
    public string ParentCdiEvent { get; set; }
    public string Reason { get; set; }
    public string ProductCode { get; set; }
    public int EvaluationId { get; set; }
    public long MemberPlanId { get; set; }
    public int ProviderId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ReceivedDate { get; set; }
}