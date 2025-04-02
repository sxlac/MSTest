namespace Signify.uACR.System.Tests.Core.Models.Database;

public class ExamNotPerformed
{
    public int ExamNotPerformedId { get; set; }
    public int ExamId { get; set; }
    public short NotPerformedReasonId { get; set; }
    public DateTime CreatedDateTime { get; set; }

}