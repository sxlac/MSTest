using System;
using System.Collections.Generic;

namespace Signify.CKD.Svc.Core.Infrastructure.Observability;

public class ObservabilityEvent
{
    public long EvaluationId { get; set; }
    public Guid EventId { get; set; }
    public string EventType { get; set; }
    public Dictionary<string, object> EventValue { get; set; }
}