using System;
using NServiceBus;
using Signify.CKD.Svc.Core.Data.Entities;

namespace Signify.CKD.Svc.Core.Events;

public abstract class ExamStatusEvent : ICommand
{
    public Guid EventId { get; set; }
    public long EvaluationId { get; set; }
    public CKDStatusCode StatusCode { get; set; }
    public DateTimeOffset StatusDateTime { get; set; }
}
