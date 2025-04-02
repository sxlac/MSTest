using Dps.Labs.Webhook.Api.Test.Library.Actions;
using Signify.Dps.Test.Utilities.CoreApi.Actions;
using Signify.Dps.Test.Utilities.Kafka.Actions;
using Signify.eGFR.System.Tests.Core.Constants;
using Signify.Dps.Test.Utilities.DataGen;
using Signify.Dps.Test.Utilities.Wiremock.Actions;
using Signify.eGFR.Core.Events.Akka;
using Signify.eGFR.Core.Events.Status;
using Signify.QE.Core.Exceptions;
using Signify.QE.Core.Utilities.NewRelic.Actions;
using ProviderPayRequestSent = Signify.eGFR.Core.Events.Status.ProviderPayRequestSent;


namespace Signify.eGFR.System.Tests.Core.Actions;

public class BaseTestActions : DatabaseActions
{
    protected readonly CoreApiActions CoreApiActions = new (CoreApiConfigs, Provider.ProviderId, TestConstants.Product, FormVersionId, LoggingHttpMessageHandler);
    public static CoreKafkaActions CoreKafkaActions ; 
    public static CoreKafkaActions CoreHomeAccessKafkaActions;
    public static WiremockActions WiremockActions;
    public static NewRelicActions NewRelicActions;
    public static WebhookApiActions WebhookApiActions;

    protected Dictionary<int, string> GenerateNotPerformedAnswers(int reasonId, string reason)
    {
        if (reasonId == Answers.ClinicallyIrrelevantAnswerId)
        {
            return new Dictionary<int, string>
            {
                { Answers.KedPerformedAnswerId, "1" }, // Is Ked Test Performed
                { Answers.EgfrNotPerformedAnswerId, "1" }, // Is egfr Test Performed
                { Answers.DosAnswerId, DateTime.Now.ToString("O") }, // Date of Service
                { Answers.ClinicallyIrrelevantAnswerId, "Clinically not relevant" }, // Reason Not Performed
                { Answers.EgfrNotesAnswerId, "ReasonNotesAnswer" }
            }; 
        }
        var parentReasonId = GetParentNotPerformedReasonId(reasonId);
        var parentReason = GetParentNotPerformedReason(reasonId);
        return new Dictionary<int, string>
        {
            { Answers.KedPerformedAnswerId, "1" }, // Is Ked Test Performed
            { Answers.EgfrNotPerformedAnswerId, "1" }, // Is egfr Test Performed
            { Answers.DosAnswerId, DateTime.Now.ToString("O") }, // Date of Service
            { parentReasonId, parentReason }, // Reason Not Performed
            { reasonId, reason }, // Reason Not Performed 2
            { Answers.EgfrNotesAnswerId, "ReasonNotesAnswer" }
        };
    }

    private static string GetParentNotPerformedReason(int reasonId)
        => GetParentNotPerformedReasonId(reasonId).Equals(Answers.UnableToPerformAnswerId)
            ? Answers.ProviderUnableAnswer
            : Answers.MemberRefusedAnswer;
    
    
    private static int GetParentNotPerformedReasonId(int reasonId)
    {
        var reasonIds = new List<int>
        {
            Answers.EnvironmentalIssueAnswerId,
            Answers.InsufficientTrainingAnswerId,
            Answers.NoSuppliesOrEquipmentAnswerId,
            Answers.TechnicalIssueAnswerId,
            Answers.MemberPhysicallyUnableAnswerId
        };
        return reasonIds.Contains(reasonId)?Answers.UnableToPerformAnswerId:Answers.MemberRefusedAnswerId;
    }

    
    protected async Task<bool> ValidateExamStatusCodesByEvaluationId(int evaluationId, List<int> expectedIds)
    {
        var exam = await GetExamByEvaluationId(evaluationId);
        return await ValidateExamStatusCodesByExamId(exam.ExamId, expectedIds);
    }

    protected static Dictionary<int, string> GeneratePerformedAnswers()
    {
        //var barcode = DataGen.RandomInt(100000, 999999).ToString();
        var numCode = DataGen.GetBarcode(InvalidLgcBarcodePattern);
        var alpha = DataGen.GetBarcode(AlphaBarcodePattern);
        var notes = DataGen.GetBarcode(Notes);
        return new Dictionary<int, string>
        {
            {Answers.EgfrPerformedAnswerId, "Yes"},
            {Answers.KedPerformedAnswerId, "Yes"},
            {Answers.DosAnswerId, DateTime.Now.ToString("O")},
            {Answers.KedAlphacodeAnswerId, alpha},
            {Answers.EgfrNotesAnswerId, notes},
            {Answers.KedNumcodeAnswerId, numCode},
        };
    }

    protected static Dictionary<int, string> GenerateKedPerformedAnswers()
    {
        var numCode = DataGen.GetBarcode(LgcBarcodePattern);
        var alpha = DataGen.GetBarcode(AlphaBarcodePattern);
        var notes = DataGen.GetBarcode(Notes);
        return new Dictionary<int, string>
        {
            {Answers.EgfrPerformedAnswerId, "Yes"},
            {Answers.KedPerformedAnswerId, "Yes"},
            {Answers.DosAnswerId, DateTime.Now.ToString("O")},
            {Answers.KedAlphacodeAnswerId, alpha},
            {Answers.EgfrNotesAnswerId, notes},
            {Answers.KedNumcodeAnswerId, numCode},
        };
    }
    
    protected async Task<ProviderPayRequestSent> GetProviderPayRequestSentEvent(int evaluationId)
    {
        try
        {
            return await CoreKafkaActions.GetEgfrProviderPayRequestSentEvent<ProviderPayRequestSent>(evaluationId);
        }
        catch (KafkaEventsNotFoundException)
        {
            return null;
        }
    }
    protected async Task<ProviderPayableEventReceived> GetProviderPayableEventReceivedEvent(int evaluationId)
    {
        try
        {
            return await CoreKafkaActions.GetEgfrProviderPayableEventReceivedEvent<ProviderPayableEventReceived>(evaluationId);
        }
        catch (KafkaEventsNotFoundException)
        {
            return null;
        }
        
    }
    protected async Task<ProviderNonPayableEventReceived> GetProviderNonPayableEventReceivedEvent(int evaluationId)
    {   
        try
        {
            return await CoreKafkaActions.GetEgfrProviderNonPayableEventReceivedEvent<ProviderNonPayableEventReceived>(evaluationId);
        }
        catch (KafkaEventsNotFoundException)
        {
            return null;
        }
        
    }
    
    protected async Task<OrderCreationEvent> GetOrderCreationEvent(int evaluationId)
    {   
        try
        {
            return await CoreKafkaActions.GetOrderCreationEvent<OrderCreationEvent>(evaluationId);
        }
        catch (KafkaEventsNotFoundException)
        {
            return null;
        }
        
    }
}