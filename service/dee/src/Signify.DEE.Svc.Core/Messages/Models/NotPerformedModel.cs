using System;

namespace Signify.DEE.Svc.Core.Messages.Models;

public sealed class NotPerformedModel : IEquatable<NotPerformedModel>
{
    public int NotPerformedReasonId { get; set; }
    public int AnswerId { get; set; }
    public string Reason { get; set; }
    public string ReasonType { get; set; }
    public string ReasonNotes { get; set; }

    public bool Equals(NotPerformedModel other)
    {
        return other != null && NotPerformedReasonId == other.NotPerformedReasonId && AnswerId == other.AnswerId && Reason == other.Reason && ReasonType == other.ReasonType && ReasonNotes == other.ReasonNotes;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((NotPerformedModel) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = NotPerformedReasonId;
            hashCode = (hashCode * 397) ^ AnswerId.GetHashCode();
            hashCode = (hashCode * 397) ^ Reason.GetHashCode();
            hashCode = (hashCode * 397) ^ ReasonType.GetHashCode();
            hashCode = (hashCode * 397) ^ (ReasonNotes != null ? ReasonNotes.GetHashCode() : 0);
            return hashCode;
        }
    }
}