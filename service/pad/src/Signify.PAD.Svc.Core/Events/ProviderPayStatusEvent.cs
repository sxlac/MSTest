using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Events;

[ExcludeFromCodeCoverage]
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
    
    /// <summary>
    /// ProviderPay paymentId
    /// </summary>
    public string PaymentId { get; set; }
}