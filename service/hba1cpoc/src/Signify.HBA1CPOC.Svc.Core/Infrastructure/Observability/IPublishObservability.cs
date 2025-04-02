namespace Signify.HBA1CPOC.Svc.Core.Infrastructure.Observability;

public interface IPublishObservability
{
    /// <summary>
    /// Adds <see cref="observabilityEvent"/> to list of observability events if <see cref="sendImmediately"/> is false
    /// else sends the event immediately to Observability Platform
    /// </summary>
    /// <param name="observabilityEvent"></param>
    /// <param name="sendImmediately">true/false; whether to send the event immediately to the platform instead of adding to a list for later submission</param>
    void RegisterEvent(ObservabilityEvent observabilityEvent, bool sendImmediately = false);

    /// <summary>
    /// Submits the list of observability events to Observability Platform
    /// </summary>
    void Commit();
}