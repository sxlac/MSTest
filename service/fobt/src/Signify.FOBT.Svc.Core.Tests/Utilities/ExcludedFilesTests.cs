using Castle.Core.Internal;
using Signify.FOBT.Svc.Core.Events;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Utilities;

public class ExcludedFilesTests
{
    [Fact]
    public void Should_Exclude_Only_Allowed_Classes()
    {
        string[] allowedClasses =
        [
            "OktaConfig", "RedisConfig", "ServiceBusConfig", "UriHealthCheckConfig", "webApiConfig",
            "KafkaPublishException", "MailSendException", "DeserializationErrorHandler", "StreamingErrorHandler", 
            "GetLatestCdiEvent", "LaunchDarklyConfig", "LaunchDarklyFlagConfig", "InvUpdateReceived", "Result", 
            "UpdateInventorySaga", "CreateLabResult", "CreateOrUpdateFOBT", "ObservabilityEvent", 
            "CreateBarcodeHistory", "AddFOBTNotPerformed", "GetProviderPayByFobtId", "QueryFOBTWithId",
            "UpdateInventorySagaData", "FobtEvalReceived", "HomeAccessResultsReceived", "BaseStatusMessage", 
            "BillRequestNotSent", "BillRequestSent", "NotPerformed", "Performed", "GetPDFToClient", 
            "BusinessRuleAnswers", "BusinessRuleStatus", "ExamStatusEvent", "FobtStatusCode", 
            "NotPerformedReasonResult", "ProviderPayStatusEvent", "BarcodeUpdate", "BillRequestAccepted", 
            "CdiEventBase", "DpsProduct", "CDIFailedEvent", "EvaluationFinalizedEvent", "Product", "Location", 
            "InventoryUpdated", "LabResultsReceivedEvent", "OrderHeld", "PdfDeliveredToClient", "RCMRequestEvent", 
            "Results", "Group", "OrderHeld", "OrderHeldContext", "ProviderNonPayableEventReceived",
            "ProviderPayableEventReceived", "ProviderPayRequestSent", "FOBTDataContext", "FOBT",
            "FOBTBarcodeHistory", "FOBTBilling", "FOBTNotPerformed", "FOBTStatus", "FOBTStatusCode",
            "LabResults", "NotPerformedReason", "PDFToClient", "ProviderPay", "AccessToken",
            "EvaluationStatusHistory", "EvaluationStatusCode", "EvaluationVersionRs", "EvaluationModel",
            "EvaluationAnswerModel", "MemberInfoRs", "MemberPhones", "ProviderInfoRs", "ProviderPayApiResponse",
            "UpdateInventoryResponse", "CreateOrder", "OktaTokenRequest", "ProviderPayApiRequest",
            "RCMBilling", "UpdateInventoryRequest", "BaseDlqMessage", "CdiEventDlqMessage", "EvaluationDlqMessage", 
            "LabsBarcodeDlqMessage", "PdfDeliveryDlqMessage", "RcmBillDlqMessage"
        ];

        var missedClasses = new List<string>();
        foreach (var type in GetTypesWithExcludeFromCodeCoverageAttribute())
        {
            if (type.IsClass && type.IsVisible
                             && !allowedClasses.Any(o => string.Equals(o, type.Name, StringComparison.OrdinalIgnoreCase)))
            {
                missedClasses.Add(type.Name);
            }
        }

        Assert.Empty(missedClasses);
    }

    private static IEnumerable<Type> GetTypesWithExcludeFromCodeCoverageAttribute()
    {
        var assembly = typeof(EvaluationFinalizedEvent).GetTypeInfo().Assembly;

        return assembly
            .GetTypes()
            .Where(type => type.GetAttributes<ExcludeFromCodeCoverageAttribute>().Any());
    }
}