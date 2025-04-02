using System;
using System.Collections.Generic;
using Signify.eGFR.Core.Constants;

namespace Signify.eGFR.Core.Events.Akka;

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

    public string ProductCode { get; } = ProductCodes.eGFR;

    /// <summary>
    /// Vendor for this Order Creation event
    /// </summary>
    public string Vendor { get; set; }

    public Dictionary<string, string> Context { get; set; }
}