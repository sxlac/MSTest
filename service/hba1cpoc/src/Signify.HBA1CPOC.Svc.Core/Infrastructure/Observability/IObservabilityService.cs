using System.Collections.Generic;

namespace Signify.HBA1CPOC.Svc.Core.Infrastructure.Observability;

public interface IObservabilityService
{
    /// <summary>
    /// Adds a custom event to New Relic with the specified name and values
    /// </summary>
    /// <param name="eventType">The name of the custom event</param>
    /// <param name="eventValue">A string dictionary containing the key-value pairs for the custom event</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    void AddEvent(string eventType, Dictionary<string, object> eventValue);
    
    /// <summary>
    /// Adds a custom attribute to New Relic with the specified name and values
    /// </summary>
    /// <param name="attributeType">The name of the custom attribute</param>
    /// <param name="attributeValue">The value of the custom attribute</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    void AddAttribute(string attributeType, object attributeValue);
}