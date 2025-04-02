namespace Signify.DEE.Svc.Core.Messages.Models;

public class ExamGraderModel
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string NPI { get; set; }
    public string Taxonomy { get; set; }

    public override string ToString()
    {
        return $"{nameof(FirstName)}: {FirstName}, {nameof(LastName)}: {LastName}, {nameof(NPI)}: {NPI}, {nameof(Taxonomy)}: {Taxonomy}";
    }

    protected bool Equals(ExamGraderModel other)
    {
        return string.Equals(FirstName, other.FirstName) && string.Equals(LastName, other.LastName) && string.Equals(NPI, other.NPI) && string.Equals(Taxonomy, other.Taxonomy);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ExamGraderModel) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (FirstName != null ? FirstName.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (LastName != null ? LastName.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (NPI != null ? NPI.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Taxonomy != null ? Taxonomy.GetHashCode() : 0);
            return hashCode;
        }
    }
}