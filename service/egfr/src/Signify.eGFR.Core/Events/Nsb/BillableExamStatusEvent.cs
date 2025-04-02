using System;

namespace EgfrNsbEvents;

public class BillableExamStatusEvent : ExamStatusEvent
{
    public Guid BillId { get; set; }
    
    public DateTimeOffset PdfDeliveryDateTime { get; set; }  
}