using Signify.uACR.Core.Constants;
using System.Diagnostics.CodeAnalysis;
using System;

namespace Signify.uACR.Core.Events.Status;

/// <summary>
/// Base class for all status events published to Kafka
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class BaseStatusMessage
{
    public string ProductCode { get; } = Application.ProductCode;
    public long EvaluationId { get; set; }
    public long MemberPlanId { get; set; }
    public int ProviderId { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public DateTimeOffset ReceivedDate { get; set; }
}