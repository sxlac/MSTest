using System.Diagnostics.CodeAnalysis;
using System;

namespace UacrNsbEvents;

[ExcludeFromCodeCoverage]
public class ProcessBillingEvent
{
    public Guid EventId { get; set; }
    public long EvaluationId { get; set; }
    public int PdfDeliveredToClientId { get; set; }
    public bool IsBillable { get; set; }
    public string RcmProductCode { get; set; }

    /// <summary>
    /// Date and Time the parent event (PdfDeliveredToClient) was created 
    /// </summary>
    public DateTimeOffset StatusDateTime { get; set; }

    public ProcessBillingEvent(Guid eventId, long evaluationId, int pdfDeliveredToClientId, bool isBillable, DateTimeOffset statusDateTime, string rcmProductCode)
    {
        EvaluationId = evaluationId;
        PdfDeliveredToClientId = pdfDeliveredToClientId;
        IsBillable = isBillable;
        EventId = eventId;
        StatusDateTime = statusDateTime;
        RcmProductCode = rcmProductCode;
    }
}