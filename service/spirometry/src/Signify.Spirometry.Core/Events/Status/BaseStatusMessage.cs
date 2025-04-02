using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.Events.Status;

/// <summary>
/// Base class for all status events published to Kafka
/// </summary>
[ExcludeFromCodeCoverage]
public class BaseStatusMessage
{
#pragma warning disable CA1822
    public string ProductCode => Constants.ProductCodes.Spirometry;
#pragma warning restore CA1822
    public long EvaluationId { get; set; }
    public long MemberPlanId { get; set; }
    public int ProviderId { get; set; }
    public DateTimeOffset CreateDate { get; set; }
    public DateTimeOffset ReceivedDate { get; set; }
}