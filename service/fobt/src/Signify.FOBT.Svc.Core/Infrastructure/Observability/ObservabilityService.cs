using System.Collections.Generic;

namespace Signify.FOBT.Svc.Core.Infrastructure.Observability;

public class ObservabilityService : IObservabilityService
{
    public void AddEvent(string eventType, Dictionary<string, object> eventValue)
    {
        NewRelic.Api.Agent.NewRelic.RecordCustomEvent(eventType, eventValue);
    }
}