using System;

namespace Signify.DEE.Svc.Core.Messages.Models;

public class EventRequestModel
{
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }

    public override string ToString()
    {
        return $"{nameof(Start)}: {Start}, {nameof(End)}: {End}";
    }

    private bool Equals(EventRequestModel other)
    {
        return Start.Equals(other.Start) && End.Equals(other.End);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((EventRequestModel) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Start.GetHashCode() * 397) ^ End.GetHashCode();
        }
    }
}