namespace Signify.DEE.Svc.Core.Constants;

public static class OutboundTopics
{
    public const string Dee_Status = "dee_status";
    public const string Dee_Results = "dee_results";
    public const string Dlq_Evaluation = "dps_evaluation_dlq";
    public const string Dlq_Pdfdelivery = "dps_pdfdelivery_dlq";
    public const string Dlq_CdiEvents = "dps_cdi_events_dlq";
    public const string Dlq_RcmBill = "dps_rcm_bill_dlq";
    public const string Dlq_CdiHolds = "dps_cdi_holds_dlq";
}