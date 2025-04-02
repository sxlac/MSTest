using Signify.uACR.Core.Constants;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System;

namespace Signify.uACR.Core.Events.Akka;

[ExcludeFromCodeCoverage]
public class OrderCreationEvent
{
    /// <summary>
    /// Identifier of this event
    /// </summary>
    public Guid EventId { get; set; }

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