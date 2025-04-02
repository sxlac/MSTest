using System;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace EgfrNsbEvents;

/// <summary>
/// A billable event has been sent for an eGFR exam
/// </summary>
public class BillRequestSentEvent
{
    /// <summary>
    /// Identifier of the event that was billable
    /// </summary>
    public Guid EventId { get; set; }
    /// <summary>
    /// Identifier of the evaluation associated with this event
    /// </summary>
    public long EvaluationId { get; set; }
    /// <summary>
    ///  BillId for event that was billable
    /// </summary>
    public Guid BillId { get; set; }
    /// <summary>
    /// ExamId for the eGFR Exam
    /// </summary>
    public int ExamId { get; set; }
    
    /// <summary>
    /// The code representing the product as it is configured in the RCM system
    /// </summary>
    /// Although nullable, required by RCM to create a bill
    public string RcmProductCode { get; set; } 
}