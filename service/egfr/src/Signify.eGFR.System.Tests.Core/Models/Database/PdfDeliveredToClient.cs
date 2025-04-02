namespace Signify.eGFR.System.Tests.Core.Models.Database;

public class PdfDeliveredToClient
{
    public Guid EventId { get; set; }
    public long EvaluationId { get; set; }
    public long BatchId { get; set; }
    public string BatchName { get; set; }
    public DateTime DeliveryDateTime { get; set; }
    public DateTime CreatedDateTime { get; set; }
}