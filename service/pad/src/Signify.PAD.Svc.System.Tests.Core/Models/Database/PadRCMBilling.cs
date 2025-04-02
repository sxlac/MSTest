namespace Signify.PAD.Svc.System.Tests.Core.Models.Database;

public class PadRCMBilling
{
    public int Id { get; set; }

    public string BillId { get; set; }
    
    public int PADId { get; set; }

    public DateTime CreatedDateTime { get; set; }

    public bool Accepted { get; set; }

    public DateTime AcceptedAt { get; set; }

}