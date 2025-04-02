namespace Signify.PAD.Svc.System.Tests.Core.Models.Database;

public class NotPerformedReason
{
    public int NotPerformedId { get; set; }
    public int PADId { get; set; }
    public int AnswerId { get; set; }
    public string Notes { get; set; }
}