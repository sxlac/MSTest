using NServiceBus;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.FOBT.Svc.Core.Events;

[ExcludeFromCodeCoverage]
public class RCMRequestEvent : IEvent
{
    public int FOBTId { get; set; }
    public int EvaluationId { get; set; }
    public int SharedClientId { get; set; }
    public int MemberPlanId { get; set; }
    public DateTime DateOfService { get; set; }
    public string UsStateOfService { get; set; }
    public int ProviderId { get; set; }
    public string RcmProductCode { get; set; }
    public string ApplicationId { get; set; }
    public DateTime BillableDate { get; set; } // Pdf Delivery date 
    public Dictionary<string, string> AdditionalDetails { get; set; }
    public string CorrelationId { get; set; }
    public string BillingProductCode { get; set; }
    public String StatusCode { get; set; }
    public Data.Entities.FOBT FOBT { get; set; }
}