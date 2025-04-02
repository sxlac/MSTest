using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Signify.eGFR.Core.Models;

namespace Signify.eGFR.Core.Data.Entities;

/// <summary>
/// Corresponds to the different status updates a eGFR exam can go through
/// </summary>
[ExcludeFromCodeCoverage]
public class ExamStatusCode
{
    public static readonly ExamStatusCode ExamPerformed = new((int)StatusCode.ExamPerformed, "eGFR Exam Performed");
    public static readonly ExamStatusCode ExamNotPerformed = new((int)StatusCode.ExamNotPerformed, "eGFR Exam Not Performed");
    public static readonly ExamStatusCode BillableEventReceived = new((int)StatusCode.BillableEventReceived, "Billable Event Received");
    public static readonly ExamStatusCode BillRequestSent = new((int)StatusCode.BillRequestSent, "Bill Request Sent");
    public static readonly ExamStatusCode ClientPdfDelivered = new((int)StatusCode.ClientPdfDelivered, "Client PDF Delivered");
    public static readonly ExamStatusCode LabResultsReceived = new((int)StatusCode.LabResultsReceived, "Lab Results Received");
    public static readonly ExamStatusCode BillRequestNotSent = new((int)StatusCode.BillRequestNotSent, "Bill Request Not Sent");
    public static readonly ExamStatusCode ProviderPayableEventReceived = new((int)StatusCode.ProviderPayableEventReceived, "ProviderPayableEventReceived");
    public static readonly ExamStatusCode ProviderPayRequestSent = new((int)StatusCode.ProviderPayRequestSent, "ProviderPayRequestSent");
    public static readonly ExamStatusCode ProviderNonPayableEventReceived = new((int)StatusCode.ProviderNonPayableEventReceived, "ProviderNonPayableEventReceived");
    public static readonly ExamStatusCode CdiPassedReceived = new((int)StatusCode.CdiPassedReceived, "CdiPassedReceived");
    public static readonly ExamStatusCode CdiFailedWithPayReceived = new((int)StatusCode.CdiFailedWithPayReceived, "CdiFailedWithPayReceived");
    public static readonly ExamStatusCode CdiFailedWithoutPayReceived = new((int)StatusCode.CdiFailedWithoutPayReceived, "CdiFailedWithoutPayReceived");
    public static readonly ExamStatusCode OrderRequested = new((int)StatusCode.OrderRequested, "Order Requested");

    public static readonly IReadOnlyCollection<ExamStatusCode> All = new List<ExamStatusCode>(new[]
    {
        ExamPerformed,
        ExamNotPerformed,
        BillableEventReceived,
        BillRequestSent,
        ClientPdfDelivered,
        LabResultsReceived,
        BillRequestNotSent,
        ProviderPayableEventReceived,
        ProviderPayRequestSent,
        ProviderNonPayableEventReceived,
        CdiPassedReceived,
        CdiFailedWithPayReceived,
        CdiFailedWithoutPayReceived,
        OrderRequested
    });

    /// <summary>
    /// Identifier of this status code
    /// </summary>
    [Key]
    public int StatusCodeId { get; init; }

    /// <summary>
    /// Short name for the status code
    /// </summary>
    public string StatusName { get; }

    internal ExamStatusCode() // Required for NServiceBus deserialization
    {
    }

    private ExamStatusCode(int statusCodeId, string statusName)
    {
        StatusCodeId = statusCodeId;
        StatusName = statusName;
    }

    public virtual ICollection<ExamStatus> ExamStatuses { get; set; } = new HashSet<ExamStatus>();
}