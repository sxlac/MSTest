using Signify.eGFR.System.Tests.Core.Models.Database;

namespace Signify.eGFR.System.Tests.Core.Constants;

public class ExamStatusCodes
{
    public static readonly ExamStatusCode ExamPerformed = new(1, "Exam Performed");
    public static readonly ExamStatusCode ExamNotPerformed = new(2, "Exam Not Performed");
    public static readonly ExamStatusCode BillableEventReceived = new(3, "Billable Event Received");
    public static readonly ExamStatusCode BillRequestSent = new(4, "Bill Request Sent");
    public static readonly ExamStatusCode ClientPDFDelivered = new(5, "Client PDF Delivered");
    public static readonly ExamStatusCode LabResultsReceived = new(6, "Lab Results Received");
    public static readonly ExamStatusCode BillRequestNotSent = new(7, "Bill Request Not Sent");
    public static readonly ExamStatusCode ProviderPayableEventReceived = new(8, "ProviderPayableEventReceived");
    public static readonly ExamStatusCode ProviderPayRequestSent = new(9, "ProviderPayRequestSent");
    public static readonly ExamStatusCode ProviderNonPayableEventReceived = new(10, "ProviderNonPayableEventReceived");
    public static readonly ExamStatusCode CDIPassedReceived  = new(11, "CDIPassedReceived");
    public static readonly ExamStatusCode CDIFailedWithPayReceived = new(12, "CDIFailedWithPayReceived");
    public static readonly ExamStatusCode CDIFailedWithoutPayReceived = new(13, "CDIFailedWithoutPayReceived"); 
    public static readonly ExamStatusCode OrderRequested = new(14, "Order Requested"); 
}