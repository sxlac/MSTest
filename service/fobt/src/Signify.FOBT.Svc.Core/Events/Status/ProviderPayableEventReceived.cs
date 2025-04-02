using System.Diagnostics.CodeAnalysis;
using Signify.FOBT.Messages.Events.Status;

namespace Signify.FOBT.Svc.Core.Events.Status;

[ExcludeFromCodeCoverage]
public class ProviderPayableEventReceived : BaseStatusMessage
{
    /// <summary>
    /// Name of the CDI event that triggered ProviderPay
    /// </summary>
    public string ParentCdiEvent { get; set; }
}