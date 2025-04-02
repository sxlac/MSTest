using System;
using System.Collections.Generic;

namespace Signify.DEE.Svc.Core.Messages.Models;

public class EventResponseModel
{
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public List<ExamEventModel> Events { get; set; }

    public override string ToString()
    {
        return $"{nameof(Start)}: {Start}, {nameof(End)}: {End}, {nameof(Events)}: {Events}";
    }

    private bool Equals(EventResponseModel other)
    {
        return Start.Equals(other.Start) && End.Equals(other.End) && Equals(Events, other.Events);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((EventResponseModel) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Start.GetHashCode();
            hashCode = (hashCode * 397) ^ End.GetHashCode();
            hashCode = (hashCode * 397) ^ (Events != null ? Events.GetHashCode() : 0);
            return hashCode;
        }
    }
}