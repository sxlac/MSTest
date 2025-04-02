namespace Signify.FOBT.Svc.System.Tests.Core.Models.Database;

public class LabResults
{
    public int FOBTId { get; set; }
    public Guid OrderCorrelationId { get; set; }
    public string Barcode { get; set; }
    public string AbnormalIndicator { get; set; }
    public string LabResult { get; set; }
    public string Exception { get; set; }
    public DateTime CollectionDate { get; set; }
    public DateTime ServiceDate { get; set; }
    public DateTime ReleaseDate { get; set; }
}