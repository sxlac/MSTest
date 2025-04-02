namespace Signify.eGFR.System.Tests.Core.Models.Kafka;

public class PdfDeliveredToClientEvent
{
    public DateTimeOffset CreatedDateTime { get; set; }
    public DateTimeOffset DeliveryDateTime { get; set; }
    public Guid EventId { get; set; }
    public long EvaluationId { get; set; }
    public List<string> ProductCodes { get; set; }
    public long BatchId { get; set; }
    public string BatchName { get; set; }
}