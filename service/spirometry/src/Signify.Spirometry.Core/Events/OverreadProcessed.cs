using System;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace SpiroEvents;

[ExcludeFromCodeCoverage]
public class OverreadProcessed
{
    /// <summary>
    /// Unique identifier for an overread
    /// </summary>
    /// <remarks>
    /// Note this is only unique for the vendor that sent this overread. This is not guaranteed
    /// to be unique across vendors.
    /// </remarks>
    public Guid? OverreadId { get; set; }

    /// <summary>
    /// Identifier of the member (ie patient receiving health services and exams)
    /// </summary>
    public long? MemberId { get; set; }

    /// <summary>
    /// Identifier of the appointment (appointment scheduled with the member) this evaluation corresponds to
    /// </summary>
    public long AppointmentId { get; set; }

    /// <summary>
    /// UTC timestamp this exam was performed
    /// </summary>
    public DateTimeOffset PerformedDateTime { get; set; }

    /// <summary>
    /// Unique identifier of the session that the spirometry tests were conducted on
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// UTC timestamp at which the overread occurred
    /// </summary>
    public DateTimeOffset OverreadDateTime { get; set; }

    /// <summary>
    /// Unique identifier of the best test conducted
    /// </summary>
    public Guid? BestTestId { get; set; }

    /// <summary>
    /// Unique identifier of the best FVC test
    /// </summary>
    public Guid? BestFvcTestId { get; set; }

    /// <summary>
    /// Comment about the best FVC test
    /// </summary>
    public string BestFvcTestComment { get; set; }

    /// <summary>
    /// Unique identifier of the best FEV-1 test
    /// </summary>
    public Guid? BestFev1TestId { get; set; }

    /// <summary>
    /// Comment about the best FEV-1 test
    /// </summary>
    public string BestFev1TestComment { get; set; }

    /// <summary>
    /// Unique identifier of the best PEF test
    /// </summary>
    public Guid? BestPefTestId { get; set; }

    /// <summary>
    /// Comment about the best PEF test
    /// </summary>
    public string BestPefTestComment { get; set; }

    /// <summary>
    /// Overread comment
    /// </summary>
    public string Comment { get; set; }

    /// <summary>
    /// FEV-1/FVC ratio as determined by the pulmonologist that conducted the overread
    /// </summary>
    public decimal Fev1FvcRatio { get; set; }

    /// <summary>
    /// The name and email of the pulmonologist that conducted the overread
    /// </summary>
    public string OverreadBy { get; set; }
    
    /// <summary>
    /// Whether there is an obstruction per the overread
    /// </summary>
    public string ObstructionPerOverread { get; set; }

    /// <summary>
    /// When the overread was received by Signify
    /// </summary>
    public DateTimeOffset ReceivedDateTime { get; set; }
}