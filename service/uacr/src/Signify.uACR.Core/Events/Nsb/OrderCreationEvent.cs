using Signify.uACR.Core.Constants;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace UacrNsbEvents;

[ExcludeFromCodeCoverage]
public class OrderCreationEvent
{
    /// <summary>
    /// Identifier of this event
    /// </summary>
    public Guid EventId { get; set; }
    
    /// <summary>
    /// Identifier of this uACR exam
    /// </summary>
    public int ExamId { get; set; }
    
    /// <summary>
    /// From the evaluation event, when the evaluation was received by the Evaluation API
    /// </summary>
    public DateTimeOffset StatusDateTime { get; set; }

    /// <summary>
    /// Identifier of this evaluation
    /// </summary>
    public long EvaluationId { get; set; }

    public string ProductCode { get; } = Application.ProductCode;

    /// <summary>
    /// Vendor for this Order Creation event
    /// </summary>
    public string Vendor { get; set; }

    public Dictionary<string, string> Context { get; set; }
}