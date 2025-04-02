using System;

namespace Signify.eGFR.Core.Events.Status;

/// <summary>
/// Base class for all status events published to Kafka
/// </summary>
public abstract class BaseStatusMessage
{
    public string ProductCode { get; } = Constants.ProductCodes.eGFR;
    public long EvaluationId { get; set; }
    public long MemberPlanId { get; set; }
    public int ProviderId { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public DateTimeOffset ReceivedDate { get; set; }
}