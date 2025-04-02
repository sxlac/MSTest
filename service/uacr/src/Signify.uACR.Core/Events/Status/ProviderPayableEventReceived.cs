using System.Diagnostics.CodeAnalysis;

namespace Signify.uACR.Core.Events.Status;

[ExcludeFromCodeCoverage]
public class ProviderPayableEventReceived : BaseStatusMessage
{
    /// <summary>
    /// Name of the CDI event that triggered ProviderPay
    /// </summary>
    public string ParentCdiEvent { get; set; }
}