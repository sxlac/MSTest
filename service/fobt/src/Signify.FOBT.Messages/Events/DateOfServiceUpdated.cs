using NServiceBus;
using System;

namespace Signify.FOBT.Messages.Events;

public class DateOfServiceUpdated : IEvent
{
    public int EvaluationId { get; }
    public DateTime DateOfService { get; }

    public DateOfServiceUpdated(int evaluationId, DateTime dateOfService)
    {
        EvaluationId = evaluationId;
        DateOfService = dateOfService;
    }
}