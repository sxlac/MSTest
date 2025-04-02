namespace Signify.HBA1CPOC.System.Tests.Core.Models.Database;

public class BillRequest: HBA1CPOC
{
    public int BillRequestId { get; set; }
    public int ExamId { get; set; }
    public String BillId { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public DateTime AcceptedAt { get; set; }
    public bool Accepted { get; set; }
    

}