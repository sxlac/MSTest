namespace Signify.eGFR.System.Tests.Core.Models.Database;

public class ExamNotPerformed
{
    public int ExamNotPerformedId { get; set; }
    public int ExamTId { get; set; }
    public int AnswerId { get; set; }
    public int NotPerformedReasonId { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public string Notes { get; set; }
}