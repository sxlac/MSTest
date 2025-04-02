namespace Signify.DEE.Svc.Core.Messages.Models;

public class ProviderModel
{
    public int ProviderId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string NationalProviderIdentifier { get; set; }
    public string PersonalEmail { get; set; }

    protected bool Equals(ProviderModel other)
    {
        return int.Equals(ProviderId, other.ProviderId) && string.Equals(FirstName, other.FirstName) && string.Equals(LastName, other.LastName)
               && string.Equals(NationalProviderIdentifier, other.NationalProviderIdentifier)
               && string.Equals(PersonalEmail, other.PersonalEmail);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ProviderModel)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (ProviderId != 0 ? ProviderId.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (FirstName != null ? FirstName.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (LastName != null ? LastName.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (PersonalEmail != null ? PersonalEmail.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (NationalProviderIdentifier != null ? NationalProviderIdentifier.GetHashCode() : 0);
            return hashCode;
        }
    }
}