using System;
using System.Collections.Generic;

namespace Signify.DEE.Svc.Core.Events;

/// <summary>
/// Evaluation finalized model
/// </summary>
public class EvaluationFinalizedEvent
{
	public Guid Id { get; set; }
	public long EvaluationId { get; set; }
	public int EvaluationTypeId { get; set; }
	public int FormVersionId { get; set; }
	public int? ProviderId { get; set; }
	public string UserName { get; set; }
	public long AppointmentId { get; set; }
	public string ApplicationId { get; set; }
	public int MemberPlanId { get; set; }
	public int MemberId { get; set; }
	public int ClientId { get; set; }
	public string DocumentPath { get; set; }
	public DateTimeOffset CreatedDateTime { get; set; }
	public DateTimeOffset ReceivedDateTime { get; set; }
	public DateTimeOffset? DateOfService { get; set; }
	public List<Product> Products { get; set; }
	public Location Location { get; set; }

	public EvaluationFinalizedEvent()
	{
	}

	public EvaluationFinalizedEvent(Guid id, long evaluationId, int evaluationTypeId, int formVersionId, int? providerId, string userName, long appointmentId, string applicationId, int memberPlanId, int memberId, int clientId, string documentPath, DateTimeOffset createdDateTime, DateTimeOffset receivedDateTime, DateTimeOffset? dateOfService, List<Product> products, Location location)
	{
		Id = id;
		EvaluationId = evaluationId;
		EvaluationTypeId = evaluationTypeId;
		FormVersionId = formVersionId;
		ProviderId = providerId;
		UserName = userName;
		AppointmentId = appointmentId;
		ApplicationId = applicationId;
		MemberPlanId = memberPlanId;
		MemberId = memberId;
		ClientId = clientId;
		DocumentPath = documentPath;
		CreatedDateTime = createdDateTime;
		ReceivedDateTime = receivedDateTime;
		DateOfService = dateOfService;
		Products = products;
		Location = location;
	}

	private bool Equals(EvaluationFinalizedEvent other)
	{
		return Id.Equals(other.Id) && EvaluationId == other.EvaluationId && EvaluationTypeId == other.EvaluationTypeId && FormVersionId == other.FormVersionId && ProviderId == other.ProviderId && UserName == other.UserName && AppointmentId == other.AppointmentId && ApplicationId == other.ApplicationId && MemberPlanId == other.MemberPlanId && MemberId == other.MemberId && ClientId == other.ClientId && DocumentPath == other.DocumentPath && CreatedDateTime.Equals(other.CreatedDateTime) && ReceivedDateTime.Equals(other.ReceivedDateTime) && Nullable.Equals(DateOfService, other.DateOfService) && Equals(Products, other.Products) && Equals(Location, other.Location);
	}

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		return obj.GetType() == this.GetType() && Equals((EvaluationFinalizedEvent) obj);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			var hashCode = Id.GetHashCode();
			hashCode = (hashCode * 397) ^ EvaluationId.GetHashCode();
			hashCode = (hashCode * 397) ^ EvaluationTypeId;
			hashCode = (hashCode * 397) ^ FormVersionId;
			hashCode = (hashCode * 397) ^ ProviderId.GetHashCode();
			hashCode = (hashCode * 397) ^ (UserName != null ? UserName.GetHashCode() : 0);
			hashCode = (hashCode * 397) ^ AppointmentId.GetHashCode();
			hashCode = (hashCode * 397) ^ (ApplicationId != null ? ApplicationId.GetHashCode() : 0);
			hashCode = (hashCode * 397) ^ MemberPlanId;
			hashCode = (hashCode * 397) ^ MemberId;
			hashCode = (hashCode * 397) ^ ClientId;
			hashCode = (hashCode * 397) ^ (DocumentPath != null ? DocumentPath.GetHashCode() : 0);
			hashCode = (hashCode * 397) ^ CreatedDateTime.GetHashCode();
			hashCode = (hashCode * 397) ^ ReceivedDateTime.GetHashCode();
			hashCode = (hashCode * 397) ^ DateOfService.GetHashCode();
			hashCode = (hashCode * 397) ^ (Products != null ? Products.GetHashCode() : 0);
			hashCode = (hashCode * 397) ^ (Location != null ? Location.GetHashCode() : 0);
			return hashCode;
		}
	}

	public override string ToString()
	{
		return $"{nameof(Id)}: {Id}, {nameof(EvaluationId)}: {EvaluationId}, {nameof(EvaluationTypeId)}: {EvaluationTypeId}, {nameof(FormVersionId)}: {FormVersionId}, {nameof(ProviderId)}: {ProviderId}, {nameof(UserName)}: {UserName}, {nameof(AppointmentId)}: {AppointmentId}, {nameof(ApplicationId)}: {ApplicationId}, {nameof(MemberPlanId)}: {MemberPlanId}, {nameof(MemberId)}: {MemberId}, {nameof(ClientId)}: {ClientId}, {nameof(DocumentPath)}: {DocumentPath}, {nameof(CreatedDateTime)}: {CreatedDateTime}, {nameof(ReceivedDateTime)}: {ReceivedDateTime}, {nameof(DateOfService)}: {DateOfService}, {nameof(Products)}: {Products}, {nameof(Location)}: {Location}";
	}
}


public class Product
{
	public string ProductCode { get; set; }

	public Product(string productCode)
	{
		ProductCode = productCode;
	}

	private bool Equals(Product other)
	{
		return ProductCode == other.ProductCode;
	}

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		return obj.GetType() == this.GetType() && Equals((Product) obj);
	}

	public override int GetHashCode()
	{
		return ProductCode != null ? ProductCode.GetHashCode() : 0;
	}

	public override string ToString()
	{
		return $"{nameof(ProductCode)}: {ProductCode}";
	}
}


public class Location
{
	public double Latitude { get; set; }
	public double Longitude { get; set; }

	public Location(double latitude, double longitude)
	{
		Latitude = latitude;
		Longitude = longitude;
	}

	private bool Equals(Location other)
	{
		return Latitude.Equals(other.Latitude) && Longitude.Equals(other.Longitude);
	}

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		return obj.GetType() == this.GetType() && Equals((Location) obj);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			return (Latitude.GetHashCode() * 397) ^ Longitude.GetHashCode();
		}
	}

	public override string ToString()
	{
		return $"{nameof(Latitude)}: {Latitude}, {nameof(Longitude)}: {Longitude}";
	}
}