namespace Signify.Spirometry.Svc.System.Tests.Core.Models.Database;

public class ProviderPay
{
    public string PaymentId { get; set; }
    public int SpirometryExamId { get; set; }
    public DateTime CreatedDateTime { get; set; }
}