using Signify.Spirometry.Svc.System.Tests.Core.Models.Database;

namespace Signify.Spirometry.Svc.System.Tests.Core.Constants;

public static class ExamStatusCodes
{
    public static readonly SpiroStatusCode ExamPerformed = new(1, "Spirometry Exam Performed");
    public static readonly SpiroStatusCode ExamNotPerformed = new(2, "Spirometry Exam Not Performed");
    public static readonly SpiroStatusCode BillableEventReceived = new(3, "Billable Event Received");
    public static readonly SpiroStatusCode BillRequestSent = new(4, "Bill Request Sent");
    public static readonly SpiroStatusCode ClientPDFDelivered = new(5, "Client PDF Delivered");
    public static readonly SpiroStatusCode BillRequestNotSent = new(6, "Bill Request Not Sent");
    public static readonly SpiroStatusCode OverreadProcessed = new(7, "Overread Processed");
    public static readonly SpiroStatusCode ResultsReceived = new(8, "Results Received");
    public static readonly SpiroStatusCode ClarificationFlagCreated = new(9, "Clarification Flag Created");
    public static readonly SpiroStatusCode ProviderPayableEventReceived = new(10, "Provider Payable Event Received");
    public static readonly SpiroStatusCode ProviderPayRequestSent = new(11, "Provider Pay Request Sent");
    public static readonly SpiroStatusCode ProviderNonPayableEventReceived = new(12, "Provider Non-Payable Event Received");
    public static readonly SpiroStatusCode CDIPassedReceived = new(13, "CDI Passed Received");
    public static readonly SpiroStatusCode CDIFailedwithPayReceived = new(14, "CDI Failed with Pay Received");
    public static readonly SpiroStatusCode CDIFailedwithoutPayReceived = new(15, "CDI Failed without Pay Received");
}