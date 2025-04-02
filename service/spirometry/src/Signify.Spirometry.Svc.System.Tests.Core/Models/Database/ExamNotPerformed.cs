namespace Signify.Spirometry.Svc.System.Tests.Core.Models.Database;

public class ExamNotPerformed
{
    public int SpirometryExamId { get; set; }
    public int NotPerformedReasonId { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public string Notes { get; set; }
    public string Reason { get; set; }
    public int AnswerId { get; set; }
}