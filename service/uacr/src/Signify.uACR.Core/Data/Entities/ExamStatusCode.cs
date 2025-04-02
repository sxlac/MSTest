using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Signify.uACR.Core.Models;

namespace Signify.uACR.Core.Data.Entities;

[ExcludeFromCodeCoverage]
public class ExamStatusCode
{
    public static readonly ExamStatusCode ExamPerformed = new((int)StatusCode.ExamPerformed, "Exam Performed");
    public static readonly ExamStatusCode ExamNotPerformed = new((int)StatusCode.ExamNotPerformed, "Exam Not Performed");
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

    public int ExamStatusCodeId { get; set; }
    public string StatusName { get; set; }

    internal ExamStatusCode() // Required for NServiceBus deserialization
    {
    }

    private ExamStatusCode(int examStatusCodeId, string statusName)
    {
        ExamStatusCodeId = examStatusCodeId;
        StatusName = statusName;
    }

    public virtual ICollection<ExamStatus> ExamStatuses { get; set; }
}