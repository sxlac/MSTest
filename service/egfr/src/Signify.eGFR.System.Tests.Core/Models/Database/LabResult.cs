namespace Signify.eGFR.System.Tests.Core.Models.Database;

public class LabResult
{
    public int LabResultId { get; set; }
    public long ExamId { get; set; }
    public DateTime ReceivedDate { get; set; }
    public decimal? EgfrResult { get; set; }
    public int NormalityIndicatorId { get; set; }
    public string ResultDescription { get; set; }
    public DateTime CreatedDate { get; set; }
}