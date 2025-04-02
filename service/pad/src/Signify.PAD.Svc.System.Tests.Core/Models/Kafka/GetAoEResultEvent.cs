namespace Signify.PAD.Svc.System.Tests.Core.Models.Kafka;

public class GetAoEResultEvent
{
    public int EvaluationId { get; set; }
    public DateTime ReceivedDate { get; set; }
    public string ProductCode { get; set; }
    public List<ClinicalSupport> ClinicalSupport { get; set; }
}

public class ClinicalSupport
{
    public string SupportType { get; set; }  // Property for ClinicalSupport Results
    public string SupportValue { get; set; }  // Property for ClinicalSupport Results
}