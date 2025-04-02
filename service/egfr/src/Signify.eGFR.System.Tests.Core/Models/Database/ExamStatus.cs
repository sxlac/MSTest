namespace Signify.eGFR.System.Tests.Core.Models.Database;

public class ExamStatus
{
    public int ExamStatusId { get; set; }
    public int ExamId { get; set; }
    public int ExamStatusCodeId { get; set; }
    public DateTime StatusDateTime { get; set; }
    public DateTime CreatedDateTime { get; set; }
}