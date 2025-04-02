using System;

namespace Signify.DEE.Svc.Core.Messages.Models;

public class ExamEventModel
{
    public Guid EventId { get; set; }
    public int ExamId { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }
    public string Status { get; set; }

    public override string ToString()
    {
        return $"{nameof(EventId)}: {EventId}, {nameof(ExamId)}: {ExamId}, {nameof(CreatedDateTime)}: {CreatedDateTime}, {nameof(Status)}: {Status}";
    }

    private bool Equals(ExamEventModel other)
    {
        return EventId.Equals(other.EventId) && ExamId == other.ExamId && CreatedDateTime.Equals(other.CreatedDateTime) && string.Equals(Status, other.Status);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((ExamEventModel) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = EventId.GetHashCode();
            hashCode = (hashCode * 397) ^ ExamId;
            hashCode = (hashCode * 397) ^ CreatedDateTime.GetHashCode();
            hashCode = (hashCode * 397) ^ (Status != null ? Status.GetHashCode() : 0);
            return hashCode;
        }
    }
}