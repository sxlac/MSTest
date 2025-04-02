using System.Diagnostics.CodeAnalysis;
using System;

namespace UacrNsbEvents;

[ExcludeFromCodeCoverage]
public class BillableExamStatusEvent : ExamStatusEvent
{
    public Guid BillId { get; set; }
    
    public DateTimeOffset PdfDeliveryDateTime { get; set; }  
}