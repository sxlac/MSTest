namespace Signify.HBA1CPOC.Messages.Events.Akka;

public class ProviderNonPayableEventReceived : ProviderPayableEventReceived
{
    /// <summary>
    /// Reason as to why the Exam is non payable
    /// </summary>
    public string Reason { get; set; }
}