using Signify.DEE.Svc.Core.Data.Entities;
using System;

namespace Signify.DEE.Svc.Core.Events.Status;

public class ExamStatusEvent
{
    public Guid EventId { get; set; }
    public long EvaluationId { get; set; }
    public int ExamId { get; set; }
    public string ProductCode { get; set; }
    public ExamStatusCode StatusCode { get; set; }

    /// <summary>
    /// The date and time when this status change event occurred.
    /// i.e the datetime contained within the incoming Kafka event
    /// </summary>
    public DateTimeOffset StatusDateTime { get; set; }

    /// <summary>
    /// Date and time the Kafka event was received by the PM
    /// </summary>
    public DateTimeOffset ParentEventReceivedDateTime { get; set; }

    public long MemberPlanId { get; set; }
    public long ProviderId { get; set; }
}