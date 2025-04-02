using System.Collections.Generic;

namespace Signify.HBA1CPOC.Svc.Core.Infrastructure.Observability;

public class ObservabilityEvent
{
    public long EvaluationId { get; set; }
    public string EventId { get; set; }
    public string EventType { get; set; }
    public Dictionary<string, object> EventValue { get; set; }
}