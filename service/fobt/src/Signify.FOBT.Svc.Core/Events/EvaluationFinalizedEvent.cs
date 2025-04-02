using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.FOBT.Svc.Core.Events;

/// <summary>
/// Evaluation finalized model
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class EvaluationFinalizedEvent
{
		
	public Guid Id { get; set; }
	public int EvaluationId { get; set; }
	public int EvaluationTypeId { get; set; }
	public int FormVersionId { get; set; }
	public int? ProviderId { get; set; }
	public string UserName { get; set; }
	public int AppointmentId { get; set; }
	public string ApplicationId { get; set; }
	public int MemberPlanId { get; set; }
	public int MemberId { get; set; }
	public int ClientId { get; set; }
	public string DocumentPath { get; set; }
	public DateTimeOffset CreatedDateTime { get; set; }
	public DateTime ReceivedDateTime { get; set; }
	public DateTime? DateOfService { get; set; }
	public List<Product> Products { get; set; }
	public Location Location { get; set; }

	private bool Equals(EvaluationFinalizedEvent other)
	{
		return Id.Equals(other.Id) && EvaluationId == other.EvaluationId && EvaluationTypeId == other.EvaluationTypeId && FormVersionId == other.FormVersionId && ProviderId == other.ProviderId && UserName == other.UserName && AppointmentId == other.AppointmentId && ApplicationId == other.ApplicationId && MemberPlanId == other.MemberPlanId && MemberId == other.MemberId && ClientId == other.ClientId && DocumentPath == other.DocumentPath && CreatedDateTime.Equals(other.CreatedDateTime) && ReceivedDateTime.Equals(other.ReceivedDateTime) && Nullable.Equals(DateOfService, other.DateOfService) && Equals(Products, other.Products) && Equals(Location, other.Location);
	}

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		if (obj.GetType() != this.GetType()) return false;
		return Equals((EvaluationFinalizedEvent) obj);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			var hashCode = Id.GetHashCode();
			hashCode = (hashCode * 397) ^ EvaluationId;
			hashCode = (hashCode * 397) ^ EvaluationTypeId;
			hashCode = (hashCode * 397) ^ FormVersionId;
			hashCode = (hashCode * 397) ^ ProviderId.GetHashCode();
			hashCode = (hashCode * 397) ^ (UserName != null ? UserName.GetHashCode() : 0);
			hashCode = (hashCode * 397) ^ AppointmentId;
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
}

[ExcludeFromCodeCoverage]
public sealed class Product
{
	public string ProductCode { get; set; }

	public Product() { }

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
		if (obj.GetType() != this.GetType()) return false;
		return Equals((Product) obj);
	}

	public override int GetHashCode()
	{
		return ProductCode != null ? ProductCode.GetHashCode() : 0;
	}
}

[ExcludeFromCodeCoverage]
public sealed class Location
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
		if (obj.GetType() != this.GetType()) return false;
		return Equals((Location) obj);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			return (Latitude.GetHashCode() * 397) ^ Longitude.GetHashCode();
		}
	}
}