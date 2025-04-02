namespace Signify.FOBT.Svc.System.Tests.Core.Models.Database;

public class BarcodeHistory
{
    public int FOBTId { get; set; }
    public Guid OrderCorrelationId { get; set; }
    public string Barcode { get; set; }
}