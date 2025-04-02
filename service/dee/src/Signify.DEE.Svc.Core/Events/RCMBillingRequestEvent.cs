using NServiceBus;
using System;
using System.Collections.Generic;

namespace Signify.DEE.Svc.Core.Events;

public class RCMBillingRequestEvent : ICommand
{
    public int ExamId { get; set; }
    public long EvaluationId { get; set; }
    public int SharedClientId { get; set; }
    public int ProviderId { get; set; }
    public string RcmProductCode { get; set; }
    public string ApplicationId { get; set; }
    public DateTimeOffset BillableDate { get; set; } // Pdf Delivery date
    public Dictionary<string, string> AdditionalDetails { get; set; }
    public string CorrelationId { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }
    public long AppointmentId { get; set; }
}