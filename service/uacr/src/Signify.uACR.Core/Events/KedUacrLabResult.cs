#nullable enable
using NServiceBus;
using System.Diagnostics.CodeAnalysis;
using System;

namespace Signify.uACR.Core.Events;

[ExcludeFromCodeCoverage]
public class KedUacrLabResult : IMessage
{
    /// <summary>
    /// Identifier of this evaluation
    /// </summary>
    public long EvaluationId { get; set; }
    
    /// <summary>
    /// Date/time sample collected by member/participant is received in lab
    /// </summary>
    public DateTimeOffset DateLabReceived { get; set; }
    
    /// <summary>
    /// Red High/Low = Abnormal; Grey=Inconclusive; All other colours = Normal. Used for the "Determination" field for Lab results A/N/U
    /// </summary>
    public string? UrineAlbuminToCreatinineRatioResultColor { get; set; }
    
    /// <summary>
    /// This is populated for the not performed reason
    /// </summary>
    public string? UrineAlbuminToCreatinineRatioResultDescription { get; set; }
    
    /// <summary>
    /// Identifier of the Lab Result
    /// </summary>
    public decimal? UacrResult { get; set; }
    
    /// <summary>
    /// Date and Time when the event was received by uACR PM
    /// </summary>
    public DateTimeOffset ReceivedByUacrDateTime { get; set; }
    
    /// <summary>
    /// Event Id for the event
    /// </summary>
    public Guid? EventId { get; set; }
}