using Signify.CKD.Svc.Core.Messages.Status;

namespace Signify.CKD.Svc.Core.Models;

public class ProviderPayableEventReceived : BaseStatusMessage
{
    /// <summary>
    /// Name of the CDI event that triggered ProviderPay
    /// </summary>
    public string ParentCdiEvent { get; set; }
}