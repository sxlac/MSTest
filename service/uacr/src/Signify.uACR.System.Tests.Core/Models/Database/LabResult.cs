namespace Signify.uACR.System.Tests.Core.Models.Database;

public class LabResult
{
    public int LabResultId { get; set; }
    public long EvaluationId { get; set; }
    public DateTime ReceivedDate { get; set; }
    public decimal? UacrResult { get; set; }
    public string ResultColor { get; set; }
    public string Normality { get; set; }
    public string NormalityCode { get; set; }
    public string ResultDescription { get; set; }
    public DateTime CreatedDate { get; set; }
}