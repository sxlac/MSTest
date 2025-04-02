using System.Collections.Generic;

namespace Signify.HBA1CPOC.Svc.Core.Infrastructure.Observability;


public class ObservabilityService : IObservabilityService
{
    public void AddEvent(string eventType, Dictionary<string, object> eventValue)
    {
        NewRelic.Api.Agent.NewRelic.RecordCustomEvent(eventType, eventValue);
    }
    public void AddAttribute(string attributeType, object attributeValue)
    {
        NewRelic.Api.Agent.NewRelic.GetAgent().CurrentTransaction.AddCustomAttribute(attributeType, attributeValue);
    }
}