using System;

namespace Signify.DEE.Svc.Core.Messages.Models;

public class MemberModel
{
    public long? MemberPlanId { get; set; }
    public string CenseoId { get; set; }
    public string HICNumber { get; set; }
    public string Gender { get; set; }
    public string FirstName { get; set; }
    public string MiddleName { get; set; }
    public string LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string EmailAddress { get; set; }
    public string AddressLineOne { get; set; }
    public string AddressLineTwo { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string ZipCode { get; set; }
    public string Client { get; set; }
    public int? ClientID { get; set; }
    public int? PlanID { get; set; }
    public string PlanName { get; set; }
    public string PlanNotes { get; set; }
    public string MemberPCP { get; set; }
    public string PCPAddress1 { get; set; }
    public string PCPAddress2 { get; set; }
    public string PCPCity { get; set; }
    public string PCPState { get; set; }
    public string PCPZip { get; set; }
    public bool IsValidAddress { get; set; }
    public bool CenseoDoNotCall { get; set; }
    public int PreferredProviderGenderId { get; set; }
    public int PreferredProviderLanguageId { get; set; }
    public Guid HouseholdIdentifier { get; set; }
    public bool IsActive { get; set; }
    public int? Age
    {
        get
        {
            var age = 0;
            if ((DateOfBirth).HasValue)
            {
                age = (DateTime.Now.Year) - (Convert.ToDateTime(DateOfBirth)).Year;
                if (DateTime.Now.DayOfYear < Convert.ToDateTime(DateOfBirth).DayOfYear)
                    age = age - 1;
            }
            return age;
        }
    }
    public string MBI { get; set; }

    public override string ToString()
    {
        return $"{nameof(MemberPlanId)}: {MemberPlanId}, {nameof(CenseoId)}: {CenseoId}, {nameof(HICNumber)}: {HICNumber}, {nameof(Gender)}: {Gender}, {nameof(FirstName)}: {FirstName}, {nameof(MiddleName)}: {MiddleName}, {nameof(LastName)}: {LastName}, {nameof(DateOfBirth)}: {DateOfBirth}, {nameof(EmailAddress)}: {EmailAddress}, {nameof(AddressLineOne)}: {AddressLineOne}, {nameof(AddressLineTwo)}: {AddressLineTwo}, {nameof(City)}: {City}, {nameof(State)}: {State}, {nameof(ZipCode)}: {ZipCode}, {nameof(Client)}: {Client}, {nameof(ClientID)}: {ClientID}, {nameof(PlanID)}: {PlanID}, {nameof(PlanName)}: {PlanName}, {nameof(PlanNotes)}: {PlanNotes}, {nameof(MemberPCP)}: {MemberPCP}, {nameof(PCPAddress1)}: {PCPAddress1}, {nameof(PCPAddress2)}: {PCPAddress2}, {nameof(PCPCity)}: {PCPCity}, {nameof(PCPState)}: {PCPState}, {nameof(PCPZip)}: {PCPZip}, {nameof(IsValidAddress)}: {IsValidAddress}, {nameof(CenseoDoNotCall)}: {CenseoDoNotCall}, {nameof(PreferredProviderGenderId)}: {PreferredProviderGenderId}, {nameof(PreferredProviderLanguageId)}: {PreferredProviderLanguageId}, {nameof(HouseholdIdentifier)}: {HouseholdIdentifier}, {nameof(IsActive)}: {IsActive}, {nameof(Age)}: {Age}, {nameof(MBI)}: {MBI}";
    }

    protected bool Equals(MemberModel other)
    {
        return MemberPlanId == other.MemberPlanId && string.Equals(CenseoId, other.CenseoId) && string.Equals(HICNumber, other.HICNumber) && string.Equals(Gender, other.Gender) && string.Equals(FirstName, other.FirstName) && string.Equals(MiddleName, other.MiddleName) && string.Equals(LastName, other.LastName) && DateOfBirth.Equals(other.DateOfBirth) && string.Equals(EmailAddress, other.EmailAddress) && string.Equals(AddressLineOne, other.AddressLineOne) && string.Equals(AddressLineTwo, other.AddressLineTwo) && string.Equals(City, other.City) && string.Equals(State, other.State) && string.Equals(ZipCode, other.ZipCode) && string.Equals(Client, other.Client) && ClientID == other.ClientID && PlanID == other.PlanID && string.Equals(PlanName, other.PlanName) && string.Equals(PlanNotes, other.PlanNotes) && string.Equals(MemberPCP, other.MemberPCP) && string.Equals(PCPAddress1, other.PCPAddress1) && string.Equals(PCPAddress2, other.PCPAddress2) && string.Equals(PCPCity, other.PCPCity) && string.Equals(PCPState, other.PCPState) && string.Equals(PCPZip, other.PCPZip) && IsValidAddress == other.IsValidAddress && CenseoDoNotCall == other.CenseoDoNotCall && PreferredProviderGenderId == other.PreferredProviderGenderId && PreferredProviderLanguageId == other.PreferredProviderLanguageId && HouseholdIdentifier.Equals(other.HouseholdIdentifier) && IsActive == other.IsActive && string.Equals(MBI, other.MBI);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((MemberModel) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = MemberPlanId.GetHashCode();
            hashCode = (hashCode * 397) ^ CenseoId?.GetHashCode() ?? 0 ;
            hashCode = (hashCode * 397) ^ HICNumber?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ Gender?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ FirstName?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ MiddleName?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ LastName?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ DateOfBirth.GetHashCode();
            hashCode = (hashCode * 397) ^ EmailAddress?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ AddressLineOne?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ AddressLineTwo?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ City?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ State?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ ZipCode?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ Client?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ ClientID.GetHashCode();
            hashCode = (hashCode * 397) ^ PlanID.GetHashCode();
            hashCode = (hashCode * 397) ^ PlanName?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ PlanNotes?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ MemberPCP?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ PCPAddress1?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ PCPAddress2?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ PCPCity?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ PCPState?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ PCPZip?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ IsValidAddress.GetHashCode();
            hashCode = (hashCode * 397) ^ CenseoDoNotCall.GetHashCode();
            hashCode = (hashCode * 397) ^ PreferredProviderGenderId;
            hashCode = (hashCode * 397) ^ PreferredProviderLanguageId;
            hashCode = (hashCode * 397) ^ HouseholdIdentifier.GetHashCode();
            hashCode = (hashCode * 397) ^ IsActive.GetHashCode();
            hashCode = (hashCode * 397) ^ MBI?.GetHashCode() ?? 0;
            return hashCode;
        }
    }
}