using System;
using System.Collections.Generic;

namespace Signify.CKD.Svc.Core.Events
{
	/// <summary>
	/// Evaluation finalized model
	/// </summary>
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
		public List<Product> Products { get; set; } = new();
		public Location Location { get; set; }
	}

	public sealed class Product
	{
		public string ProductCode { get; set; }

		public Product(string productCode)
		{
			ProductCode = productCode;
		}
	}

	public sealed class Location
	{
		public double Latitude { get; set; }
		public double Longitude { get; set; }

		public Location(double latitude, double longitude)
		{
			Latitude = latitude;
			Longitude = longitude;
		}
	}
}
