using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class NotPerformed
{
    public NotPerformed()
    {
    }
    
    public NotPerformed(int notPerformedId, int padId, int answerId, string notes)
    {
        NotPerformedId = notPerformedId;
        PADId = padId;
        AnswerId = answerId;
        Notes = notes;
    }

    public int NotPerformedId { get; set; }
    //Foreign key
    public virtual PAD PAD { get; set; }
        
    public int PADId { get; set; }

    public int AnswerId { get; set; }

    public string Notes { get; set; }

    protected bool Equals(NotPerformed other)
        => NotPerformedId == other.NotPerformedId && PADId == other.PADId && AnswerId.Equals(other.AnswerId) &&
           Notes.Equals(other.Notes);

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((NotPerformed)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = NotPerformedId;
            hashCode = (hashCode * 397) ^ PADId.GetHashCode();
            hashCode = (hashCode * 397) ^ AnswerId.GetHashCode();
            hashCode = (hashCode * 397) ^ Notes.GetHashCode();
            return hashCode;
        }
    }

    public override string ToString()
        => $"{nameof(NotPerformedId)}: {NotPerformedId}, {nameof(PADId)}: {PADId}, {nameof(AnswerId)}: {AnswerId}, , {nameof(Notes)}: {Notes}";
}