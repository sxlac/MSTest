namespace Signify.CKD.Svc.Core.Constants;

public static class Observability
{
    public static class EventParams
    {
        public const string EvaluationId = "EvaluationId";
        public const string CdiEvent = "CdiEvent";
        public const string PayableCdiEvents = "PayableCdiEvents";
        public const string NonPayableCdiEvents = "NonPayableCdiEvents";
        public const string NonPayableReason = "NonPayableReason";
        public const string CreatedDateTime = "CreatedDateTime";
        public const string BillId = "BillId";
        public const string StatusCode = "StatusCode";
        public const string Type = "Type";
        public const string ExpirationDateNullAttribute = "ExpirationDateNull";
        public const string DateOfServiceNullAttribute = "DateOfServiceNull";
        public const string ExpirationAfterDateOfServiceAttribute = "ExpirationDateAfterDateOfService";
    }

    public static class ProviderPay
    {
        // New Relic Custom Events
        public const string ProviderPayOrBillingEvent = "DpsProviderPayOrBilling";
        public const string ProviderPayApiStatusCodeEvent = "DpsProviderPayApiStatusCode";
        public const string RcmBillingApiStatusCodeEvent = "DpsRcmBillingApiStatusCode";
        public const string CdiEvents = "DpsCdiEvents";
        public const string PayableCdiEvents = "DpsPayableCdiEvents";
        public const string NonPayableCdiEvents = "DpsNonPayableCdiEvents";
        public const string ExpiredKits = "DpsInvalidKits";
    }

    public static class Evaluation
    {
        // New Relic Custom Events
        public const string EvaluationFinalizedEvent = "DpsEvaluationFinalized";
        public const string EvaluationDosUpdatedEvent = "DpsEvaluationDosUpdated";
        public const string EvaluationReceivedEvent = "DpsEvaluationReceived";
        public const string EvaluationPerformedEvent = "DpsEvaluationPerformed";
        public const string EvaluationNotPerformedEvent = "DpsEvaluationNotPerformed";
        public const string EvaluationUndefinedEvent = "DpsEvaluationUndefined";
        public const string EvaluationClarificationEvent = "DpsEvaluationClarification";
    }

    public static class PdfDelivered
    {
        // New Relic Custom Events
        public const string PdfDeliveryReceivedEvent = "DpsPdfDeliveryReceived";
    }


    public static class RcmBilling
    {
        // New Relic Custom Events
        public const string BillRequestRaisedEvent = "DpsBillRequestRaised";
        public const string BillRequestFailedEvent = "DpsBillRequestFailed";
    }
}