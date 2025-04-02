using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NServiceBus;

namespace Signify.PAD.Svc.Core.Events;

/// <summary>
/// The Kafka event present in the rcm_bill topic.
/// The same event will be used by NSB also.
/// Ref: https://wiki.signifyhealth.com/display/BILL/RCM+Bill+Events
/// </summary>
[ExcludeFromCodeCoverage]
public class BillRequestAccepted : ICommand
{
    /// <summary>
    /// Bill Id of the billing event
    /// </summary>
    public Guid RCMBillId { get; set; }

    /// <summary>
    /// Billing product code
    /// </summary>
    public string RCMProductCode { get; set; }

    /// <summary>
    /// A dictionary containing key value pairs.
    /// Some of the most commonly present info are BatchName and EvaluationId
    /// </summary>
    public Dictionary<string, string> AdditionalDetails { get; set; } = new();
}