using NServiceBus;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Events;

[ExcludeFromCodeCoverage]
public sealed class DateOfServiceUpdated : IEvent
{
    public int EvaluationId { get; set; }
    public DateTime DateOfService { get; set; }

    public DateOfServiceUpdated(int evaluationId, DateTime dateOfService)
    {
        EvaluationId = evaluationId;
        DateOfService = dateOfService;
    }

    private bool Equals(DateOfServiceUpdated other)
    {
        return EvaluationId == other.EvaluationId && DateOfService.Equals(other.DateOfService);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((DateOfServiceUpdated)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (EvaluationId * 397) ^ DateOfService.GetHashCode();
        }
    }

    public override string ToString()
    {
        return $"{nameof(EvaluationId)}: {EvaluationId}, {nameof(DateOfService)}: {DateOfService}";
    }
}