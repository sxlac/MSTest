using System;
using NServiceBus;

namespace EgfrNsbEvents;

public class CreateBillEvent : IMessage
{
    /// <summary>
    /// Identifier of the event that was billable
    /// </summary>
    public Guid EventId { get; set; }
    /// <summary>
    /// Identifier of the evaluation this bill corresponds to
    /// </summary>
    public long EvaluationId { get; set; }
    /// <summary>
    /// Timestamp of when the event was billable
    /// </summary>
    public DateTimeOffset BillableDate { get; set; }
    
    public string BatchName { get; set; }
    
    public DateTimeOffset PdfDeliveryDateTime { get; set; }  
    
    /// <summary>
    /// The code representing the product as it is configured in the RCM system
    /// </summary>
    /// Although nullable, required by RCM to create a bill
    public string RcmProductCode { get; set; } 
}