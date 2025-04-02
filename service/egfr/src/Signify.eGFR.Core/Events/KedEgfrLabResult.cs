using System;
using NServiceBus;

namespace Signify.eGFR.Core.Events;

public class KedEgfrLabResult : IMessage
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
    /// This is populated when a test is Grey/Inconclusive. It will be null for green/red results. 
    /// </summary>
    public string EstimatedGlomerularFiltrationRateResultDescription { get; set; }
    
    /// <summary>
    /// Used to determine Normality and Normality code
    /// </summary>
    public decimal? EgfrResult { get; set; }
    
    /// <summary>
    /// Date and Time when the event was received by eGFR PM
    /// </summary>
    public DateTimeOffset ReceivedByEgfrDateTime { get; set; }
    
    /// <summary>
    /// Event Id for the event
    /// </summary>
    public Guid? EventId { get; set; }
}