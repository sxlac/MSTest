using System;

namespace Signify.DEE.Svc.Core.Messages.Models;

public class PdfToClientModel
{

    public int PdfDeliverId { get; set; }
    public string EventId { get; set; }
    public long EvaluationId { get; set; }
    public DateTimeOffset DeliveryDateTime { get; set; }
    public DateTimeOffset DeliveryCreatedDateTime { get; set; }
    public long BatchId { get; set; }
    public string BatchName { get; set; }
    public int ExamId { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }
}