using Castle.Core.Internal;
using Signify.PAD.Svc.Core.Events;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Utilities;

public class ExcludedFilesTests
{
    [Fact]
    public void Should_Exclude_Only_Allowed_Classes()
    {
        string[] allowedClasses =
        [
            "OktaConfig", "RedisConfig", "ServiceBusConfig", "UriHealthCheckConfig", "webApiConfig",
            "KafkaPublishException", "WaveformConfig", "WaveformFailedDirectoryConfig", "DeserializationErrorHandler",
            "StreamingErrorHandler", "LaunchDarklyConfig", "LaunchDarklyFlagConfig", "AddExamStatusResponse",
            "BillRequestSent", "CreateOrUpdatePAD", "CreatePad", "NotPerformed", "ParseAoeSymptomResults",
            "Performed", "ProcessPendingWaveformResult", "ProviderPayRequest", "ProviderPayRequestSent",
            "PublishAoEResult", "PublishResults", "PublishStatusUpdateOld", "PublishStatusUpdateHandlerOld",
            "CreateOrUpdatePDFToClient", "BaseDlqMessage", "CdiEventDlqMessage", "EvaluationDlqMessage",
            "PdfDeliveryDlqMessage", "RcmBillDlqMessage", "EvalReceived", "AoeResult", "AoeSymptomAnswers",
            "BusinessRuleAnswers", "BusinessRuleStatus", "ClinicalSupport", "EvaluationAnswers",
            "EvaluationDocumentModel", "PadStatusCode", "ProviderNonPayableEventReceived", "ProviderPayableEventReceived",
            "BillRequestAccepted", "CdiEventBase", "DpsProduct", "CDIFailedEvent", "DateOfServiceUpdated",
            "EvaluationFinalizedEvent", "Product", "Location", "ExamStatusEvent", "ExamStatusEventNew", "PADPerformed",
            "PdfDeliveredToClient", "ProviderPayStatusEvent", "RcmBillingRequest", "ResultsReceived", "BaseStatusMessage",
            "BillRequestNotSent", "PADDataContext", "AoeSymptomSupportResult", "LateralityCode", "PAD", "PADRCMBilling",
            "PADStatus", "PDFToClient", "ProviderPay", "SeverityLookup", "WaveformDocument", "WaveformDocumentVendor",
            "Application", "NotPerformedReason", "EvaluationStatus", "Observability", "PadDiagnosisConfirmedClinicallyQuestion",
            "PadTestPerformedQuestion", "PadTestingResultsLeftQuestion", "PadTestingResultsRightQuestion", 
            "MemberRefusedNotesQuestion", "ReasonMemberRefusedQuestion", "ReasonNotPerformedQuestion",
            "ReasonUnableToPerformQuestion", "UnableToPerformNotesQuestion", "AoeDiagnosisConfirmedQuestion",
            "LegPainQuestion", "LegPainResolvedByMovementQuestion", "LegPainResolvedByOtcMedicationQuestion",
            "PedalPulsesQuestion", "ReasonAoeWithRestingLegPainNotConfirmedQuestion", "WaveformConfigVendor", "AccessToken",
            "EvaluationStatusHistory", "EvaluationStatusCode", "EvaluationVersionRs", "EvaluationModel", "EvaluationAnswerModel",
            "MemberInfoRs", "ProviderPayApiResponse", "ProviderRs", "CreateBillRequest", "EvaluationRequest",
            "OktaTokenRequest", "ProviderPayApiRequest"
        ];

        var lst = GetClasses();
        var missedClasses = (from cls in lst where cls.IsClass && cls.IsVisible && !allowedClasses.Any(o => string.Equals(o, cls.Name, StringComparison.OrdinalIgnoreCase)) select cls.Name).ToList();

        Assert.Empty(missedClasses);
    }

    private static IEnumerable<Type> GetClasses()
    {
        var lst = new List<Type>();
        var assembly = typeof(EvaluationFinalizedEvent).GetTypeInfo().Assembly;
        foreach (var type in assembly.GetTypes())
        {
            if (type.GetAttributes<ExcludeFromCodeCoverageAttribute>().Any())
            {
                lst.Add(type);
            }
        }
        return lst;
    }
}