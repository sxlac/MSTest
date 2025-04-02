using System;
using System.Diagnostics.CodeAnalysis;
using NServiceBus;

namespace Signify.eGFR.Core.Commands;

[ExcludeFromCodeCoverage]
public class SaveProviderPay : ICommand
{
    public Guid EventId { get; set; }
    public long EvaluationId { get; set; }
    public string PaymentId { get; set; }
    public string ParentEvent { get; set; }
    public int  ExamId { get; set; }
    public long MemberPlanId { get; set; }
    public long ProviderId { get; set; }

    /// <summary>
    /// Date and time contained within the Kafka event that triggered this NSB
    /// </summary>
    public DateTimeOffset ParentEventDateTime { get; set; }

    /// <summary>
    /// Date and time the Kafka event that triggered this Nsb was received by PM
    /// </summary>
    public DateTimeOffset ParentEventReceivedDateTime { get; set; }
}