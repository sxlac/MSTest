namespace Signify.HBA1CPOC.Messages.Events.Status;

public class ProviderPayStatusEvent : ExamStatusEvent
{
    /// <summary>
    /// Name of the CDI event that triggered ProviderPay
    /// </summary>
    public string ParentCdiEvent { get; set; }

    /// <summary>
    /// Reason as to why the Exam is non payable, if non-payable
    /// </summary>
    public string Reason { get; set; }
    public string PaymentId { get; set; }
}