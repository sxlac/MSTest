using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.Spirometry.Core.Events.Akka;

/// <summary>
/// Event received over Kafka from the Signify Evaluation service
///
/// See https://chgit.censeohealth.com/projects/EV/repos/evaluationsapi/browse/src/CH.Evaluation.Events/EvaluationFinalizedEvent.cs
/// </summary>
[ExcludeFromCodeCoverage]
public class EvaluationFinalizedEvent
{
    /// <summary>
    /// Identifier of this event
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Identifier of this evaluation
    /// </summary>
    public int EvaluationId { get; set; }

    public int EvaluationTypeId { get; set; }

    /// <summary>
    /// Version of the form that was completed on the provider's mobile device
    /// </summary>
    public int FormVersionId { get; set; }

    /// <summary>
    /// Identifier of the provider that performed the evaluation services
    /// </summary>
    public int ProviderId { get; set; }

    public string UserName { get; set; }

    /// <summary>
    /// Identifier of the appointment (the appointment scheduled with the member) this evaluation corresponds to
    /// </summary>
    public long AppointmentId { get; set; }

    /// <summary>
    /// Details about the physical location where the evaluation was performed
    /// </summary>
    public Location Location { get; set; }

    /// <summary>
    /// Identifier of the application (source) that created this identifier
    /// </summary>
    public string ApplicationId { get; set; }

    /// <summary>
    /// Identifier of the member's health care plan
    /// </summary>
    public int MemberPlanId { get; set; }

    /// <summary>
    /// Identifier of the member (ie patient receiving health services and exams)
    /// </summary>
    public long MemberId { get; set; }

    /// <summary>
    /// Identifier of the client (ie insurance company)
    /// </summary>
    public int ClientId { get; set; }

    public string DocumentPath { get; set; }

    /// <summary>
    /// Date and time the evaluation was started by the provider
    /// </summary>
    public DateTimeOffset CreatedDateTime { get; set; }

    /// <summary>
    /// Date and time the evaluation was received by Signify Health
    /// </summary>
    public DateTime ReceivedDateTime { get; set; }

    /// <summary>
    /// Date the provider performed the service for the member
    /// </summary>
    public DateTime? DateOfService { get; set; }

    /// <summary>
    /// List of products that were included in this evaluation
    /// </summary>
    public ICollection<Product> Products { get; set; } = new List<Product>();

    /// <summary>
    /// Abbreviated US state where the evaluation was performed
    /// </summary>
    public string StateAbbreviation { get; set; }

    public int? PlanId { get; set; }

    public int Version { get; set; }
}

/// <summary>
/// Product information
/// </summary>
/// <remarks>DPS add-ons (ex Spirometry, FIT, PAD) are products</remarks>
[ExcludeFromCodeCoverage]
public class Product
{
    public string ProductCode { get; set; }
}

/// <summary>
/// Details about the physical location where the evaluation was performed
/// </summary>
[ExcludeFromCodeCoverage]
public class Location
{
    public double Latitude { get; set; }

    public double Longitude { get; set; }
}