using NServiceBus;
using Signify.uACR.Core.Data.Entities;
using System.Diagnostics.CodeAnalysis;
using System;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace UacrNsbEvents;

[ExcludeFromCodeCoverage]
public class ExamStatusEvent : IMessage
{
    public Guid EventId { get; set; }

    public long EvaluationId { get; set; }

    public int ExamId { get; set; }

    public ExamStatusCode StatusCode { get; set; }

    /// <summary>
    /// The date and time when this status change event occurred.
    /// i.e the datetime contained within the incoming Kafka event
    /// </summary>
    public DateTimeOffset StatusDateTime { get; set; }

    /// <summary>
    /// Date and time the Kafka event was received by the PM
    /// </summary>
    public DateTimeOffset ParentEventReceivedDateTime { get; set; }

    public string Barcode { get; set; }
    public long MemberPlanId { get; set; }
    public long ProviderId { get; set; }
    
    /// <summary>
    /// The code representing the product as it is configured in the RCM system
    /// </summary>
    /// Although nullable, required by RCM to create a bill
    public string RcmProductCode { get; set; } 
}