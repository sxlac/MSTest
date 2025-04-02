using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.HBA1CPOC.Svc.Core.Data.Entities;

// ReSharper disable once InconsistentNaming
#pragma warning disable S3897 // SonarQube - Implement 'IEquatable<HBA1CPOC>'
#pragma warning disable S4035 // SonarQube - Seal class 'HBA1CPOC' or implement 'IEqualityComparer<T>' instead
#pragma warning disable S101 // SonarQube - Types should be named in PascalCase
[ExcludeFromCodeCoverage]
public class HBA1CPOC
#pragma warning restore S101
#pragma warning restore S4035
#pragma warning restore S3897
{
	public int HBA1CPOCId { get; set; }
	public int? EvaluationId { get; set; }
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
	public DateOnly? DateOfBirth { get; set; }
	public string AddressLineOne { get; set; }
	public string AddressLineTwo { get; set; }
	public string City { get; set; }
	public string State { get; set; }
	public string ZipCode { get; set; }
	public string NationalProviderIdentifier { get; set; }
	public string A1CPercent { get; set; }
	public string NormalityIndicator { get; set; }
	public DateOnly? ExpirationDate { get; set; }

	public virtual ICollection<HBA1CPOCStatus> ExamStatuses { get; set; } = new List<HBA1CPOCStatus>();

	public bool Equals(HBA1CPOC other)
	{
		return HBA1CPOCId == other.HBA1CPOCId && EvaluationId == other.EvaluationId && MemberPlanId == other.MemberPlanId
		       && MemberId == other.MemberId && CenseoId == other.CenseoId && AppointmentId == other.AppointmentId 
		       && ProviderId == other.ProviderId && Nullable.Equals(DateOfService, other.DateOfService) 
		       && CreatedDateTime.Equals(other.CreatedDateTime) && ReceivedDateTime.Equals(other.ReceivedDateTime) 
		       && ClientId == other.ClientId && UserName == other.UserName && ApplicationId == other.ApplicationId 
		       && FirstName == other.FirstName && MiddleName == other.MiddleName && LastName == other.LastName 
		       && Nullable.Equals(DateOfBirth, other.DateOfBirth) && AddressLineOne == other.AddressLineOne 
		       && AddressLineTwo == other.AddressLineTwo && City == other.City && State == other.State 
		       && ZipCode == other.ZipCode && NationalProviderIdentifier == other.NationalProviderIdentifier 
		       && A1CPercent == other.A1CPercent && Nullable.Equals(ExpirationDate, other.ExpirationDate)
		       && NormalityIndicator == other.NormalityIndicator;
	}

	public override bool Equals(object obj)
	{
		if (obj is null) return false;
		if (ReferenceEquals(this, obj)) return true;
		return obj.GetType() == this.GetType() && Equals((HBA1CPOC) obj);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			var hashCode = HBA1CPOCId;
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
			hashCode = (hashCode * 397) ^ (A1CPercent != null ? A1CPercent.GetHashCode() : 0);
			hashCode = (hashCode * 397) ^ (NormalityIndicator != null ? NormalityIndicator.GetHashCode() : 0);
			hashCode = (hashCode * 397) ^ ExpirationDate.GetHashCode();
			return hashCode;
		}
	}

	public override string ToString()
	{
		return $"{nameof(HBA1CPOCId)}: {HBA1CPOCId}, {nameof(EvaluationId)}: {EvaluationId}, {nameof(MemberPlanId)}: {MemberPlanId}, {nameof(MemberId)}: {MemberId}, {nameof(CenseoId)}: {CenseoId}, {nameof(AppointmentId)}: {AppointmentId}, {nameof(ProviderId)}: {ProviderId}, {nameof(DateOfService)}: {DateOfService}, {nameof(CreatedDateTime)}: {CreatedDateTime}, {nameof(ReceivedDateTime)}: {ReceivedDateTime}, {nameof(ClientId)}: {ClientId}, {nameof(UserName)}: {UserName}, {nameof(ApplicationId)}: {ApplicationId}, {nameof(FirstName)}: {FirstName}, {nameof(MiddleName)}: {MiddleName}, {nameof(LastName)}: {LastName}, {nameof(DateOfBirth)}: {DateOfBirth}, {nameof(AddressLineOne)}: {AddressLineOne}, {nameof(AddressLineTwo)}: {AddressLineTwo}, {nameof(City)}: {City}, {nameof(State)}: {State}, {nameof(ZipCode)}: {ZipCode}, {nameof(NationalProviderIdentifier)}: {NationalProviderIdentifier}, {nameof(A1CPercent)}: {A1CPercent}, {nameof(ExpirationDate)}: {ExpirationDate}";
	}
}