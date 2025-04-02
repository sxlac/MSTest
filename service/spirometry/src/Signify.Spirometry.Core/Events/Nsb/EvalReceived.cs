using System;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace SpiroNsbEvents;

[ExcludeFromCodeCoverage]
public class EvalReceived
{
    /// <summary>
    /// Identifier of this event
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Identifier of the application (source) that created this identifier
    /// </summary>
    public string ApplicationId { get; set; }

    /// <summary>
    /// Identifier of this evaluation
    /// </summary>
    public int EvaluationId { get; set; }

    /// <summary>
    /// Identifier of the appointment (the appointment scheduled with the member) this evaluation corresponds to
    /// </summary>
    public long AppointmentId { get; set; }

    /// <summary>
    /// Identifier of the provider that performed the evaluation services
    /// </summary>
    public int ProviderId { get; set; }

    /// <summary>
    /// Identifier of the member (ie patient receiving health services and exams)
    /// </summary>
    public long MemberId { get; set; }

    /// <summary>
    /// Identifier of the member's health care plan
    /// </summary>
    public int MemberPlanId { get; set; }

    /// <summary>
    /// Identifier of the client (ie insurance company)
    /// </summary>
    public int ClientId { get; set; }

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
    /// Date and time this Spirometry process manager received the evaluation event
    /// </summary>
    public DateTime ReceivedBySpirometryProcessManagerDateTime { get; set; }
}