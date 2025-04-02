using System;

namespace Signify.DEE.Svc.Core.Messages.Models;

public class IrisPatientModel
{
    public long PatientId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Gender { get; set; }
    public DateTime BirthDate { get; set; }
    public string State { get; set; }

    public override string ToString()
    {
        return $"{nameof(PatientId)}: {PatientId}, {nameof(FirstName)}: {FirstName}, {nameof(LastName)}: {LastName}, {nameof(Gender)}: {Gender}, {nameof(BirthDate)}: {BirthDate}, {nameof(State)}: {State}";
    }

    protected bool Equals(IrisPatientModel other)
    {
        return PatientId == other.PatientId && string.Equals(FirstName, other.FirstName) && string.Equals(LastName, other.LastName) && string.Equals(Gender, other.Gender) && BirthDate.Equals(other.BirthDate) && string.Equals(State, other.State);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((IrisPatientModel) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = PatientId.GetHashCode();
            hashCode = (hashCode * 397) ^ (FirstName != null ? FirstName.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (LastName != null ? LastName.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Gender != null ? Gender.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ BirthDate.GetHashCode();
            hashCode = (hashCode * 397) ^ (State != null ? State.GetHashCode() : 0);
            return hashCode;
        }
    }
}