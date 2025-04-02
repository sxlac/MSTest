namespace Signify.uACR.System.Tests.Core.Models.Database;

public class BillRequest
{
    public int BillRequestId { get; set; }
    public int ExamId { get; set; }
    public Guid BillId { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public DateTime AcceptedAt { get; set; }
    

}