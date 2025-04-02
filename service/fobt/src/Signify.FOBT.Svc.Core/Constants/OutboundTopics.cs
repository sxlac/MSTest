namespace Signify.FOBT.Svc.Core.Constants;

public static class OutboundTopics
{
    public const string FOBT_Status = "FOBT_Status";
    public const string FOBT_Result = "FOBT_Results";
    public const string Dlq_Evaluation = "dps_evaluation_dlq";
    public const string Dlq_LabsBarcode = "dps_labs_barcode_dlq";
    public const string Dlq_LabsHolds = "dps_labs_holds_dlq";
    public const string Dlq_HomeaccessLabresults = "dps_homeaccess_labresults_dlq";
    public const string Dlq_Pdfdelivery = "dps_pdfdelivery_dlq";
    public const string Dlq_CdiEvents = "dps_cdi_events_dlq";
    public const string Dlq_RcmBill = "dps_rcm_bill_dlq";
}