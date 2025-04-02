using System;
using System.ComponentModel.DataAnnotations;

namespace Signify.DEE.Svc.Core.Data.Entities;

public partial class PDFToClient
{

    public PDFToClient()
    {
    }

    [Key]
    public int PDFDeliverId { get; set; }
    public string EventId { get; set; }
    public long EvaluationId { get; set; }
    public DateTimeOffset DeliveryDateTime { get; set; }
    public DateTimeOffset DeliveryCreatedDateTime { get; set; }
    public long BatchId { get; set; }
    public string BatchName { get; set; }
    public int ExamId { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }

    public static PDFToClient Create(string eventId, DateTimeOffset deliveryDateTime, DateTimeOffset deliveryCreatedDateTime, long evaluationId, long batchId, string batchName, int examId, DateTimeOffset createdDateTime)
    {
        var pdfToClient = new PDFToClient
        {
            EventId = eventId,
            DeliveryCreatedDateTime = deliveryCreatedDateTime,
            DeliveryDateTime = deliveryDateTime,
            EvaluationId = evaluationId,
            BatchId = batchId,
            BatchName = batchName,
            ExamId = examId,
            CreatedDateTime = createdDateTime
        };

        return pdfToClient;
    }
}