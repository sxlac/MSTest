namespace Signify.HBA1CPOC.System.Tests.Core.Models.Database;

public class HBA1CPOCNotPerformed
{
    public int HBA1CPOCNotPerformedId { get; set; }
    public int HBA1CPOCId { get; set; }
    public short NotPerformedReasonId { get; set; }
    public DateTime CreatedDateTime { get; set; }  
    public DateTimeOffset ReceivedDateTime { get; set; }  
}