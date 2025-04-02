using System.Diagnostics.CodeAnalysis;

namespace Signify.FOBT.Svc.Core.Models;

[ExcludeFromCodeCoverage]
public class ProviderPayStatusEvent : ExamStatusEvent
{
    /// <summary>
    /// Name of the CDI event that triggered ProviderPay
    /// </summary>
    public string ParentCdiEvent { get; set; }
    public string PaymentId { get; set; }
    public string Reason { get; set; }
}