using System;
using System.Collections.Generic;

namespace Signify.eGFR.Core.Events.Akka;

/// <summary>
/// Event received over Kafka from the Signify Evaluation service
///
/// See https://chgit.censeohealth.com/projects/EV/repos/evaluationsapi/browse/src/CH.Evaluation.Events/EvaluationFinalizedEvent.cs
/// </summary>
public class EvaluationFinalizedEvent
{
    private DateTimeOffset? _dateOfService;
    
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
    public int AppointmentId { get; set; }

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
    public DateTimeOffset ReceivedDateTime { get; set; }

    /// <summary>
    /// Date the provider performed the service for the member
    /// DateOfService does not contain TimeZone in Kafka Event
    /// </summary>
    public DateTimeOffset? DateOfService {
        get => _dateOfService;
        set => _dateOfService = value.HasValue ? new DateTimeOffset(value.Value.DateTime, TimeSpan.Zero) : null;
    }

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
/// <remarks>DPS add-ons (ex eGFR, FIT, PAD) are products</remarks>
public class Product
{
    public string ProductCode { get; set; }
}

/// <summary>
/// Details about the physical location where the evaluation was performed
/// </summary>
public class Location
{
    public double Latitude { get; set; }

    public double Longitude { get; set; }
}