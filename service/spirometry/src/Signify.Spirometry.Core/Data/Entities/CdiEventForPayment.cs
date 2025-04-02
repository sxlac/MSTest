using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.Data.Entities;

/// <summary>
/// Details of the events from cdi_events topic.
/// Events can be CDIPassedEvent or CDIFailedEvent
/// </summary>
[ExcludeFromCodeCoverage]
public class CdiEventForPayment
{
    /// <summary>
    /// PK identifier of this entity
    /// </summary>
    public int CdiEventForPaymentId { get; set; }

    /// <summary>
    /// Type of the cdi event i.e. CDIPassedEvent or CDIFailedEvent
    /// </summary>
    public string EventType { get; set; }

    /// <summary>
    /// EvaluationId of the event
    /// </summary>
    public long EvaluationId { get; set; }

    /// <summary>
    /// Unique identifier of the event
    /// </summary>
    public Guid RequestId { get; set; }

    public string ApplicationId { get; set; }

    /// <summary>
    /// Whether the Provider should be paid for the IHE.
    /// Field present only for CDIFailedEvent
    /// </summary>
    public bool? PayProvider { get; set; }

    /// <summary>
    /// Reason why CDI failed.
    /// Field present only for CDIFailedEvent
    /// </summary>
    public string Reason { get; set; }

    /// <summary>
    /// Date and time contained within the CDI Event
    /// </summary>
    public DateTimeOffset DateTime { get; set; }

    /// <summary>
    /// UTC Time when the event was created within the PM's database
    /// </summary>
    public DateTimeOffset CreatedDateTime { get; set; }
}