using System;
using System.Diagnostics.CodeAnalysis;

namespace Signify.FOBT.Svc.Core.Data.Entities;

#pragma warning disable S101 // SonarQube - Types should be named in PascalCase
[ExcludeFromCodeCoverage]
public class FOBT
#pragma warning restore S101
{
	public int FOBTId { get; set; }
	public int? EvaluationId { get; set; }
	public int? MemberPlanId { get; set; }
	public int? MemberId { get; set; }
	public string CenseoId { get; set; }
	public int? AppointmentId { get; set; }
	public int? ProviderId { get; set; }
	public DateTime? DateOfService { get; set; }
	public DateTimeOffset CreatedDateTime { get; set; }
	public DateTime ReceivedDateTime { get; set; }
	public string Barcode { get; set; }
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
	public Guid? OrderCorrelationId { get; set; }

	public FOBT()
	{
	}

	public FOBT(int fobtId, int? evaluationId, int? memberPlanId, int? memberId, string censeoId, int? appointmentId, int? providerId, DateTime? dateOfService, DateTimeOffset createdDateTime, DateTime receivedDateTime, string barcode, int? clientId, string userName, string applicationId, string firstName, string middleName, string lastName, DateTime? dateOfBirth, string addressLineOne, string addressLineTwo, string city, string state, string zipCode, string nationalProviderIdentifier, Guid? orderCorrelationId)
	{
		FOBTId = fobtId;
		EvaluationId = evaluationId;
		MemberPlanId = memberPlanId;
		MemberId = memberId;
		CenseoId = censeoId;
		AppointmentId = appointmentId;
		ProviderId = providerId;
		DateOfService = dateOfService;
		CreatedDateTime = createdDateTime;
		ReceivedDateTime = receivedDateTime;
		Barcode = barcode;
		ClientId = clientId;
		UserName = userName;
		ApplicationId = applicationId;
		FirstName = firstName;
		MiddleName = middleName;
		LastName = lastName;
		DateOfBirth = dateOfBirth;
		AddressLineOne = addressLineOne;
		AddressLineTwo = addressLineTwo;
		City = city;
		State = state;
		ZipCode = zipCode;
		NationalProviderIdentifier = nationalProviderIdentifier;
		OrderCorrelationId = orderCorrelationId;
	}
	public bool Equals(FOBT other)
	{
		return FOBTId == other.FOBTId && EvaluationId == other.EvaluationId && MemberPlanId == other.MemberPlanId && MemberId == other.MemberId && CenseoId == other.CenseoId && AppointmentId == other.AppointmentId && ProviderId == other.ProviderId && Nullable.Equals(DateOfService, other.DateOfService) && CreatedDateTime.Equals(other.CreatedDateTime) && ReceivedDateTime.Equals(other.ReceivedDateTime) && Barcode == other.Barcode && ClientId == other.ClientId && UserName == other.UserName && ApplicationId == other.ApplicationId && FirstName == other.FirstName && MiddleName == other.MiddleName && LastName == other.LastName && Nullable.Equals(DateOfBirth, other.DateOfBirth) && AddressLineOne == other.AddressLineOne && AddressLineTwo == other.AddressLineTwo && City == other.City && State == other.State && ZipCode == other.ZipCode && NationalProviderIdentifier == other.NationalProviderIdentifier && OrderCorrelationId == other.OrderCorrelationId;
	}

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		if (obj.GetType() != this.GetType()) return false;
		return Equals((FOBT) obj);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			var hashCode = FOBTId;
			hashCode = (hashCode * 397) ^ EvaluationId.GetHashCode();
			hashCode = (hashCode * 397) ^ MemberPlanId.GetHashCode();
			hashCode = (hashCode * 397) ^ MemberId.GetHashCode();
			hashCode = (hashCode * 397) ^ (CenseoId != null ? CenseoId.GetHashCode() : 0);
			hashCode = (hashCode * 397) ^ AppointmentId.GetHashCode();
			hashCode = (hashCode * 397) ^ ProviderId.GetHashCode();
			hashCode = (hashCode * 397) ^ DateOfService.GetHashCode();
			hashCode = (hashCode * 397) ^ CreatedDateTime.GetHashCode();
			hashCode = (hashCode * 397) ^ ReceivedDateTime.GetHashCode();
			hashCode = (hashCode * 397) ^ (Barcode != null ? Barcode.GetHashCode() : 0);
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
			hashCode = (hashCode * 397) ^ (OrderCorrelationId != null ? OrderCorrelationId.GetHashCode() : 0);
			return hashCode;
		}
	}

	public override string ToString()
	{
		return $"{nameof(FOBTId)}: {FOBTId}, {nameof(EvaluationId)}: {EvaluationId}, {nameof(MemberPlanId)}: {MemberPlanId}, {nameof(MemberId)}: {MemberId}, {nameof(CenseoId)}: {CenseoId}, {nameof(AppointmentId)}: {AppointmentId}, {nameof(ProviderId)}: {ProviderId}, {nameof(DateOfService)}: {DateOfService}, {nameof(CreatedDateTime)}: {CreatedDateTime}, {nameof(ReceivedDateTime)}: {ReceivedDateTime}, {nameof(Barcode)}: {Barcode}, {nameof(ClientId)}: {ClientId}, {nameof(UserName)}: {UserName}, {nameof(ApplicationId)}: {ApplicationId}, {nameof(FirstName)}: {FirstName}, {nameof(MiddleName)}: {MiddleName}, {nameof(LastName)}: {LastName}, {nameof(DateOfBirth)}: {DateOfBirth}, {nameof(AddressLineOne)}: {AddressLineOne}, {nameof(AddressLineTwo)}: {AddressLineTwo}, {nameof(City)}: {City}, {nameof(State)}: {State}, {nameof(ZipCode)}: {ZipCode}, {nameof(NationalProviderIdentifier)}: {NationalProviderIdentifier}, {nameof(OrderCorrelationId)}: {OrderCorrelationId}";
	}
}