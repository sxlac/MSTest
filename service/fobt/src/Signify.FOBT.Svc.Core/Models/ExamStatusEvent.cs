using System;
using System.Diagnostics.CodeAnalysis;
using Signify.FOBT.Svc.Core.Data.Entities;

namespace Signify.FOBT.Svc.Core.Models;

[ExcludeFromCodeCoverage]
public class ExamStatusEvent
{
    public Data.Entities.FOBT Exam { get; set; }
    public Guid EventId { get; set; }
    public long EvaluationId { get; set; }
    public FOBTStatusCode StatusCode { get; set; }
    /// <summary>
    /// The date and time when this status change event occurred.
    /// i.e the datetime contained within the incoming Kafka event
    /// </summary>
    public DateTimeOffset StatusDateTime { get; set; }
    /// <summary>
    /// Date and time the Kafka event was received by the PM
    /// </summary>
    public DateTimeOffset ParentEventReceivedDateTime { get; set; }
}