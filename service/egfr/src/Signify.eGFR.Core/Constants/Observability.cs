namespace Signify.eGFR.Core.Constants;

public static class Observability
{
    public static class EventParams
    {
        public const string EvaluationId = "EvaluationId";
        public const string CdiEvent = "CdiEvent";
        public const string PayableCdiEvents = "PayableCdiEvents";
        public const string NonPayableCdiEvents = "NonPayableCdiEvents";
        public const string NonPayableReason = "NonPayableReason";
        public const string PaymentId = "PaymentId";
        public const string StatusCode = "StatusCode";
        public const string Type = "Type";
        public const string TypeProviderPay = "ProviderPay";
        public const string TypeRcmBilling = "RcmBilling";
        public const string CreatedDateTime = "CreatedDateTime";
        public const string BillId = "BillId";
        public const string OrderCreationEvents = "OrderCreationEvents";
        public const string Vendor = "Vendor";
        public const string Barcode = "Barcode";
        public const string Message = "Message";
        public const string CenseoId = "CenseoId";
        public const string CollectionDate = "CollectionDate";
        public const string LabResultId = "LabResultId";
        public const string TestNames = "TestNames";
        public const string EventId = "EventId";
        public const string Result = "Result";
        public const string IsBillable = "IsBillable";
        public const string NotPerformedReason = "NotPerformedReason";
        public const string Determination = "Determination";
    }

    public static class ProviderPay
    {
        public const string ProviderPayOrBillingEvent = "DpsProviderPayOrBilling";
        public const string ProviderPayApiStatusCodeEvent = "DpsProviderPayApiStatusCode";
        public const string RcmBillingApiStatusCodeEvent = "DpsRcmBillingApiStatusCode";
        public const string CdiEvents = "DpsCdiEvents";
        public const string PayableCdiEvents = "DpsPayableCdiEvents";
        public const string NonPayableCdiEvents = "DpsNonPayableCdiEvents";
    }

    public static class OmsOrderCreation
    {
        public const string OrderCreationEvents = "DpsOrderCreationEvents";
        public const string OrderNotRequested = "DpsOrderCreationNotRequestedEvents";
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
        public const string EvaluationCanceledEvent = "DpsEvaluationCanceled";
        public const string MissingEvaluationEvent = "DpsEvaluationMissing";
    }

    public static class PdfDelivered
    {
        // New Relic Custom Events
        public const string PdfDeliveryReceivedEvent = "DpsPdfDeliveryReceived";
        public const string PdfDeliveryReceivedEventButExamNotPerformed = "DpsPdfDeliveryReceivedButExamNotPerformed";
    }


    public static class RcmBilling
    {
        // New Relic Custom Events
        public const string BillRequestRaisedEvent = "DpsBillRequestRaised";
        public const string BillRequestFailedEvent = "DpsBillRequestFailed";
        public const string BillAcceptedSuccessEvent = "DpsBillAcceptedSuccess";
        public const string BillAcceptedNotFoundEvent = "DpsBillAcceptedNotFound";
        public const string BillAcceptedNotTrackedEvent = "DpsBillAcceptedNotTracked";
    }
    
    public static class ApiIssues
    {
        public const string ExternalApiFailureEvent = "DpsExternalApiFailure";
    }
    
        
    public static class LabResult
    {
        // New Relic Custom Events
        public const string LabResultExamDoesNotExist = "DpsLabResultExamDoesNotExist";
        public const string LabResultAlreadyExists = "DpsLabResultAlreadyExists";
        public const string LabResultReceived = "DpsLabResultReceived";
        public const string LabResultReceivedButExamNotPerformed = "DpsLabResultReceivedButExamNotPerformed";
        public const string InternalLabResultReceived = "DpsInternalLabResultReceived";
    }

    public static class RmsIlrApi
    {
        public const string GetLabResultByLabResultIdEvents = "DpsRmsIlrApiGetLabResultByLabResultIdEvents";
        public const string LabResultMappedEvents = "DpsLabResultMappedEvents";
    }
    
    public static class DeserializationErrors
    {
        public const string ErrorPublishedToDlqEvent = "DpsDeserializationErrorPublishedToDlq";
        public const string MessageKey = "MessageKey";
        public const string Topic = "Topic";
        public const string PublishingApplication = "PublishingApplication";
        public const string PublishTime = "PublishTime";
    }
}