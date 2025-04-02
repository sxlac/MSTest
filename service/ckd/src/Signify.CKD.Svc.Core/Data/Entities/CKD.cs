using System;

namespace Signify.CKD.Svc.Core.Data.Entities
{
    public class CKD
	{
        public int CKDId { get; set; }
		public long? EvaluationId { get; set; }
		public int? MemberPlanId { get; set; }
		public int? MemberId { get; set; }
		public string CenseoId { get; set; }
		public int? AppointmentId { get; set; }
		public int? ProviderId { get; set; }
		public DateTime? DateOfService { get; set; }
		public DateTimeOffset CreatedDateTime { get; set; }
		public DateTime ReceivedDateTime { get; set; }
		public int? ClientId { get; set; }
		public string UserName { get; set; }
		public string ApplicationId { get; set; }
		public string FirstName { get; set; }
		public string MiddleName { get; set; }
		public string LastName { get; set; }
		public DateTime? DateOfBirth { get; set; }
		public string AddressLineOne { get; set; }
		public string AddressLineTwo { get; set; }
		public string City { get; set; }
		public string State { get; set; }
		public string ZipCode { get; set; }
		public string NationalProviderIdentifier { get; set; }
		public string CKDAnswer { get; set; }
		public DateTime? ExpirationDate { get; set; }

		private bool Equals(CKD other)
		{
			return CKDId == other.CKDId && EvaluationId == other.EvaluationId && MemberPlanId == other.MemberPlanId && MemberId == other.MemberId && CenseoId == other.CenseoId && AppointmentId == other.AppointmentId && ProviderId == other.ProviderId && Nullable.Equals(DateOfService, other.DateOfService) && CreatedDateTime.Equals(other.CreatedDateTime) && ReceivedDateTime.Equals(other.ReceivedDateTime) && ClientId == other.ClientId && UserName == other.UserName && ApplicationId == other.ApplicationId && FirstName == other.FirstName && MiddleName == other.MiddleName && LastName == other.LastName && Nullable.Equals(DateOfBirth, other.DateOfBirth) && AddressLineOne == other.AddressLineOne && AddressLineTwo == other.AddressLineTwo && City == other.City && State == other.State && ZipCode == other.ZipCode && NationalProviderIdentifier == other.NationalProviderIdentifier && CKDAnswer == other.CKDAnswer && Nullable.Equals(ExpirationDate, other.ExpirationDate);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((CKD) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = CKDId;
				hashCode = (hashCode * 397) ^ EvaluationId.GetHashCode();
				hashCode = (hashCode * 397) ^ MemberPlanId.GetHashCode();
				hashCode = (hashCode * 397) ^ MemberId.GetHashCode();
				hashCode = (hashCode * 397) ^ (CenseoId != null ? CenseoId.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ AppointmentId.GetHashCode();
				hashCode = (hashCode * 397) ^ ProviderId.GetHashCode();
				hashCode = (hashCode * 397) ^ DateOfService.GetHashCode();
				hashCode = (hashCode * 397) ^ CreatedDateTime.GetHashCode();
				hashCode = (hashCode * 397) ^ ReceivedDateTime.GetHashCode();
				hashCode = (hashCode * 397) ^ ClientId.GetHashCode();
				hashCode = (hashCode * 397) ^ (UserName != null ? UserName.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (ApplicationId != null ? ApplicationId.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (FirstName != null ? FirstName.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (MiddleName != null ? MiddleName.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (LastName != null ? LastName.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ DateOfBirth.GetHashCode();
				hashCode = (hashCode * 397) ^ (AddressLineOne != null ? AddressLineOne.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (AddressLineTwo != null ? AddressLineTwo.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (City != null ? City.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (State != null ? State.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (ZipCode != null ? ZipCode.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (NationalProviderIdentifier != null ? NationalProviderIdentifier.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (CKDAnswer != null ? CKDAnswer.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ ExpirationDate.GetHashCode();
				return hashCode;
			}
		}

		public override string ToString()
		{
			return $"{nameof(CKDId)}: {CKDId}, {nameof(EvaluationId)}: {EvaluationId}, {nameof(MemberPlanId)}: {MemberPlanId}, {nameof(MemberId)}: {MemberId}, {nameof(CenseoId)}: {CenseoId}, {nameof(AppointmentId)}: {AppointmentId}, {nameof(ProviderId)}: {ProviderId}, {nameof(DateOfService)}: {DateOfService}, {nameof(CreatedDateTime)}: {CreatedDateTime}, {nameof(ReceivedDateTime)}: {ReceivedDateTime}, {nameof(ClientId)}: {ClientId}, {nameof(UserName)}: {UserName}, {nameof(ApplicationId)}: {ApplicationId}, {nameof(FirstName)}: {FirstName}, {nameof(MiddleName)}: {MiddleName}, {nameof(LastName)}: {LastName}, {nameof(DateOfBirth)}: {DateOfBirth}, {nameof(AddressLineOne)}: {AddressLineOne}, {nameof(AddressLineTwo)}: {AddressLineTwo}, {nameof(City)}: {City}, {nameof(State)}: {State}, {nameof(ZipCode)}: {ZipCode}, {nameof(NationalProviderIdentifier)}: {NationalProviderIdentifier}, {nameof(CKDAnswer)}: {CKDAnswer}, {nameof(ExpirationDate)}: {ExpirationDate}";
		}
	}
}
