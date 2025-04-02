namespace Signify.eGFR.Core.Events.Status;

public class ProviderPayableEventReceived : BaseStatusMessage
{
    /// <summary>
    /// Name of the CDI event that triggered ProviderPay
    /// </summary>
    public string ParentCdiEvent { get; set; }
}