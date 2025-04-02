using NServiceBus;

namespace FobtNsbEvents;

public class HomeAccessResultsReceived : IMessage
{
    public Guid EventId { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public Guid OrderCorrelationId { get; set; }
    public string Barcode { get; set; }
    public string LabTestType { get; set; }
    public string LabResults { get; set; }
    public string AbnormalIndicator { get; set; }
    public string Exception { get; set; }
    public DateTime? CollectionDate { get; set; }
    public DateTime? ServiceDate { get; set; }
    public DateTime? ReleaseDate { get; set; }
}