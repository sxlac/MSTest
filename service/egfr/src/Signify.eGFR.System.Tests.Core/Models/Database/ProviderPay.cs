namespace Signify.eGFR.System.Tests.Core.Models.Database;

public class ProviderPay
{
    public int ProviderPayId { get; set; }
    public string PaymentId { get; set; }
    public int ExamId { get; set; }
    public DateTime CreatedDateTime { get; set; }
}