using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.Constants;

[ExcludeFromCodeCoverage]
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
        public const string Message = "Message";
        public const string PaymentId = "PaymentId";
        public const string StatusCode = "StatusCode";
        public const string TypeRcmBilling = "RcmBilling";
        public const string Type = "Type";
    }

    public static class Evaluation
    {
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

    public static class ProviderPay
    {
        public const string ProviderPayOrBillingEvent = "DpsProviderPayOrBilling";
        public const string ProviderPayApiStatusCodeEvent = "DpsProviderPayApiStatusCode";
        public const string RcmBillingApiStatusCodeEvent = "DpsRcmBillingApiStatusCode";
        public const string CdiEvents = "DpsCdiEvents";
        public const string PayableCdiEvents = "DpsPayableCdiEvents";
        public const string NonPayableCdiEvents = "DpsNonPayableCdiEvents";
    }

    public static class Waveform
    {
        public const string WaveformDocumentPendingEvent = "DpsWaveformDocumentPending";
        public const string WaveformDocumentIncomingEvent = "DpsWaveformDocumentIncoming";
    }

    public static class PdfDelivered
    {
        public const string PdfDeliveryReceivedEvent = "DpsPdfDeliveryReceived";
    }

    public static class RcmBilling
    {
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

    public static class ParseNotPerformedResult
    {
        public const string TechnicalIssue = "DpsPadNotPerformedTechnicalIssue";
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