using Signify.HBA1CPOC.Messages.Events.Status;

namespace Signify.HBA1CPOC.Messages.Events.Akka;

public class ProviderPayableEventReceived : BaseStatusMessage
{
    /// <summary>
    /// Name of the CDI event that triggered ProviderPay
    /// </summary>
    public string ParentCdiEvent { get; set; }
}