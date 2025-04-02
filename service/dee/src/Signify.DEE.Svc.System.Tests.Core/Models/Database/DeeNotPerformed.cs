namespace Signify.DEE.Svc.System.Tests.Core.Models.Database;

public class DeeNotPerformed
{
    public int DeeNotPerformedId { get; set; }
    public int ExamId { get; set; }
    public int NotPerformedReasonId { get; set; }
    public string Notes { get; set; }
}