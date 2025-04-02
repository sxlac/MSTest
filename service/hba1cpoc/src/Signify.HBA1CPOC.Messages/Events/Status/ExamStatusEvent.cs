using System;
using NServiceBus;

namespace Signify.HBA1CPOC.Messages.Events.Status;

public abstract class ExamStatusEvent : ICommand
{
    public Guid EventId { get; set; }
    public long EvaluationId { get; set; }
    public int StatusCode { get; set; }
    
    public DateTimeOffset StatusDateTime { get; set; }
}