using System;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace SpiroNsbEvents;

/// <summary>
/// A billable event has occurred for a Spirometry exam
/// </summary>
[ExcludeFromCodeCoverage]
public class BillableEvent
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
    /// Timestamp of when the event was billable
    /// </summary>
    public DateTime BillableDate { get; set; }
    public string BatchName { get; set; }
}