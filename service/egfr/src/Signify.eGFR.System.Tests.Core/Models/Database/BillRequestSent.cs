namespace Signify.eGFR.System.Tests.Core.Models.Database;

public class BillRequestSent
{
    public int BillRequestId { get; set; }
    public int ExamId { get; set; }
    public Guid BillId { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public DateTime AcceptedAt { get; set; }
    public Boolean Accepted { get; set; }
    public string BillingProductCode { get; set; }
}