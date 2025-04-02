namespace Signify.CKD.Svc.Core.Models;

public class ProviderNonPayableEventReceived : ProviderPayableEventReceived
{
    /// <summary>
    /// Reason as to why the Exam is non payable
    /// </summary>
    public string Reason { get; set; }
}