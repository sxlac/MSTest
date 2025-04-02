using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Signify.FOBT.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public partial class PDFToClient
{

    public PDFToClient()
    {

    }

    [Key]
    public int PDFDeliverId { get; set; }
    public string EventId { get; set; }
    public long EvaluationId { get; set; }
    public DateTime DeliveryDateTime { get; set; }
    public DateTime DeliveryCreatedDateTime { get; set; }
    public long BatchId { get; set; }
    public string BatchName { get; set; }
    public int FOBTId { get; set; }
    public DateTime CreatedDateTime { get; set; }

    public static PDFToClient Create(string eventId, DateTime deliveryDateTime, DateTime deliveryCreatedDateTime, long evaluationId, long batchId, string batchName, int fOBTId, DateTime createdDateTime)
    {
        var pdftoClient = new PDFToClient
        {
            EventId = eventId,
            DeliveryCreatedDateTime = deliveryCreatedDateTime,
            DeliveryDateTime = deliveryDateTime,
            EvaluationId = evaluationId,
            BatchId = batchId,
            BatchName = batchName,
            FOBTId = fOBTId,
            CreatedDateTime = createdDateTime
        };

        return pdftoClient;
    }
}