using Signify.FOBT.Svc.Core.Constants;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.FOBT.Messages.Events.Status;

/// <summary>
/// Base class for all status events published to Kafka
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class BaseStatusMessage
{
    public string ProductCode => ApplicationConstants.PRODUCT_CODE;
    public int? EvaluationId { get; set; }
    public long? MemberPlanId { get; set; }
    public int? ProviderId { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public DateTimeOffset ReceivedDate { get; set; }
}