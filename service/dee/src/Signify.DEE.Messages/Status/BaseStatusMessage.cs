using System;

namespace Signify.DEE.Messages.Status;

/// <summary>
/// Base class for all status events published to Kafka
/// </summary>
public class BaseStatusMessage
{
    public string ProductCode { get; set; }
    public long EvaluationId { get; set; }
    public long MemberPlanId { get; set; }
    public int ProviderId { get; set; }
    public DateTimeOffset CreateDate { get; set; }
    public DateTimeOffset ReceivedDate { get; set; }
    public string RetinalImageTestingNotes { get; set; }
}