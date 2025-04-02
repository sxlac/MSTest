using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Events.Status;

/// <summary>
/// Base class for all status events published to Kafka
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class BaseStatusMessage
{
    public string ProductCode { get; } = Constants.Application.ProductCode;
    public long EvaluationId { get; set; }
    public long MemberPlanId { get; set; }
    public int ProviderId { get; set; }
    public DateTimeOffset CreateDate { get; set; }
    public DateTimeOffset ReceivedDate { get; set; }
}