using System;

namespace Signify.DEE.Svc.Core.Messages.Models;

public class ExamStatusModel
{
    public int ExamId { get; set; }
    public DateTimeOffset CreatedDateTime { get; set; }
    public DateTimeOffset ReceivedDateTime { get; set; }
    public string Status { get; set; }
    public Guid? DeeEventId { get; set; }

    public override string ToString()
    {
        return $"{nameof(ExamId)}: {ExamId}, {nameof(CreatedDateTime)}: {CreatedDateTime}, {nameof(ReceivedDateTime)}: {ReceivedDateTime}, {nameof(Status)}: {Status}, {nameof(DeeEventId)}: {DeeEventId}";
    }

    protected bool Equals(ExamStatusModel other)
    {
        return ExamId == other.ExamId && CreatedDateTime.Equals(other.CreatedDateTime) && ReceivedDateTime.Equals(other.ReceivedDateTime) && string.Equals(Status, other.Status) && DeeEventId.Equals(other.DeeEventId);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((ExamStatusModel) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = ExamId;
            hashCode = (hashCode * 397) ^ CreatedDateTime.GetHashCode();
            hashCode = (hashCode * 397) ^ ReceivedDateTime.GetHashCode();
            hashCode = (hashCode * 397) ^ (Status != null ? Status.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ DeeEventId.GetHashCode();
            return hashCode;
        }
    }
}