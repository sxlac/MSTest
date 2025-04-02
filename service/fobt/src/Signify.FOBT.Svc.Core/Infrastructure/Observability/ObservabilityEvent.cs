using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.FOBT.Svc.Core.Infrastructure.Observability;

[ExcludeFromCodeCoverage]
public class ObservabilityEvent
{
    public long EvaluationId { get; set; }
    public string EventId { get; set; }
    public string EventType { get; set; }
    public Dictionary<string, object> EventValue { get; set; }
}