using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Events;

/// <summary>
/// Evaluation finalized model
/// </summary>
[ExcludeFromCodeCoverage]
public class EvaluationFinalizedEvent
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
	public DateTimeOffset ReceivedDateTime { get; set; }
	public DateTime? DateOfService { get; set; }
	public List<Product> Products { get; set; } = new();
	public Location Location { get; set; }
}

[ExcludeFromCodeCoverage]
public class Product
{
	public string ProductCode { get; set; }
}

[ExcludeFromCodeCoverage]
public class Location
{
	public double Latitude { get; set; }
	public double Longitude { get; set; }
}