namespace Signify.FOBT.Svc.System.Tests.Core.Models.Database;

public class FOBTNotPerformed
{
    public int FOBTNotPerformedId { get; set; }
    public int FOBTId { get; set; }
    public int AnswerId { get; set; }
    public int NotPerformedReasonId { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public string Notes { get; set; }
}