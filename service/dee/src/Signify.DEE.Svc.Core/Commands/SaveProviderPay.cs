using NServiceBus;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.DEE.Svc.Core.Commands;

[ExcludeFromCodeCoverage]
public class SaveProviderPay : ICommand
{
    public Guid EventId { get; set; }
    public long EvaluationId { get; set; }
    public string PaymentId { get; set; }
    public DateTimeOffset ParentEventDateTime { get; set; }
    public DateTimeOffset ParentEventReceivedDateTime { get; set; }
    public string ParentEvent { get; set; }
    public int ExamId { get; set; }
    public long MemberPlanId { get; set; }
    public long ProviderId { get; set; }
    public string ProviderPayProductCode { get; set; }
}