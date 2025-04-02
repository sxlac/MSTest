namespace Signify.eGFR.Core.Events.Status;

public class ProviderNonPayableEventReceived : BaseStatusMessage
{
    /// <summary>
    /// Name of the CDI event that triggered ProviderPay
    /// </summary>
    public string ParentCdiEvent { get; set; }

    /// <summary>
    /// Reason as to why the Exam is non payable
    /// </summary>
    public string Reason { get; set; }
}