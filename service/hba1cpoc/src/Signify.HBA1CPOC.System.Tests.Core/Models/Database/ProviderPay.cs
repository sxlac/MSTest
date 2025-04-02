namespace Signify.HBA1CPOC.System.Tests.Core.Models.Database;

public class ProviderPay: HBA1CPOC
{
    public int ProviderPayId { get; set; }
    public String PaymentId { get; set; }
    public int HBA1CPOCId { get; set; }
    public DateTime CreatedDateTime { get; set; }
}