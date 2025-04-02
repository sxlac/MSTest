using System.Diagnostics.CodeAnalysis;

namespace Signify.HBA1CPOC.Svc.Core.Constants;

[ExcludeFromCodeCoverage]
public static class ApplicationConstants
{
    public const string AppName = "HBA1CPOCService";
    public const string ProductCode = "HBA1CPOC";
    public const string ItemNumber = "HBA1CPOC"; //ItemNumber in WASP Inventory
    public const string AzureServiceBus = "AzureServiceBus";
    public const string EvaluationId = "EvaluationId";
    
    public static class OutboundTopics
    {
        public const string HbA1cPoc_Status = "A1CPOC_Status";
        public const string HbA1cPoc_Result = "A1CPOC_Results";
        public const string HbA1cPoc_Performed = "A1CPOC_performed";
        public const string DpsEvaluationDlq = "dps_evaluation_dlq";
        public const string DpsPdfDeliveryDlq = "dps_pdf_delivery_dlq";
        public const string DpsCdiEventDlq = "dps_cdi_events_dlq";
        public const string DpsRcmBillDlq = "dps_rcm_bill_dlq";
    }
}