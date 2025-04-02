using Dps.Labs.Webhook.Api.Test.Library.Actions;
using Signify.Dps.Test.Utilities.CoreApi.Actions;
using Signify.Dps.Test.Utilities.DataGen;
using Signify.Dps.Test.Utilities.Kafka.Actions;
using Signify.Dps.Test.Utilities.Wiremock.Actions;
using Signify.QE.Core.Exceptions;
using Signify.QE.Core.Utilities.NewRelic.Actions;
using Signify.uACR.Core.Events.Status;
using Signify.uACR.System.Tests.Core.Constants;
using Signify.uACR.System.Tests.Core.Models.NewRelic;
using static Signify.uACR.System.Tests.Core.Constants.TestConstants;

namespace Signify.uACR.System.Tests.Core.Actions;

public class BaseTestActions : DatabaseActions
{
    protected readonly CoreApiActions CoreApiActions = new (CoreApiConfigs, Provider.ProviderId, Product, FormVersionId, LoggingHttpMessageHandler);
    public static CoreKafkaActions CoreKafkaActions;
    public static CoreKafkaActions LhaKafkaActions;
    public static WiremockActions WiremockActions;
    public static NewRelicActions NewRelicActions;
    public static WebhookApiActions WebhookApiActions;
    protected static string GetAlphaCode()
        => DataGen.GetBarcode(AlphaCodePattern);

    protected static string GetBarcode()
        => DataGen.GetBarcode(BarCodePattern);

    protected static string GetInvalidAlphaCode()
        => DataGen.GetBarcode(InvalidAlphaCodePattern);

    protected static string GetInvalidBarcode()
        => DataGen.GetBarcode(InvalidBarCodePattern);

    protected static Dictionary<int, string> GeneratePerformedAnswers(string barCode, string alphaCode)
        => new()
        {
            {Answers.KedPerformedAnswerId, "1"}, // Is Ked Test Performed
            {Answers.UacrPerformedAnswerId, "1"}, // Is uacr Test Performed
            {Answers.DoSAnswerId, DateTime.Now.ToString("O")}, // Date of Service
            {Answers.BarcodeAnswerId, barCode}, // Barcode
            {Answers.AlphacodeAnswerId, alphaCode}, // AlphaCode
        };

    protected Dictionary<int, string> GeneratePerformedAnswers()
        => GeneratePerformedAnswers(GetBarcode(), GetAlphaCode());

    protected Dictionary<int, string> GenerateNotPerformedAnswers(int reasonId, string reason)
    {
        var parentReasonId = GetParentNotPerformedReasonId(reasonId);
        var parentReason = GetParentNotPerformedReason(reasonId);
        return new Dictionary<int, string>
        {
            { Answers.KedPerformedAnswerId, "1" }, // Is Ked Test Performed
            { Answers.UacrNotPerformedAnswerId, "1" }, // Is uacr Test Performed
            { Answers.DoSAnswerId, DateTime.Now.ToString("O") }, // Date of Service
            { parentReasonId, parentReason }, // Reason Not Performed
            { reasonId, reason }, // Reason Not Performed 2
            { Answers.ReasonNotesAnswerId, Answers.ReasonNotesAnswer }
        };
    }
    
    protected async Task ValidateExamStatusCodesByEvaluationId(int evaluationId, List<int> statusCodeIds)
    {
        var exam = await GetExamByEvaluationId(evaluationId);
        await ValidateExamStatusCodesByExamId(exam.ExamId, statusCodeIds);
    }

    protected static string GetParentNotPerformedReason(int reasonId)
    {
        return GetParentNotPerformedReasonId(reasonId).Equals(Answers.ProviderReasonAnswerId)||GetParentNotPerformedReasonId(reasonId).Equals(Answers.KedUnableToPerformAnswerId)? Answers.ProviderUnableAnswer
            : Answers.MemberRefusedAnswer;
    }
    

    protected static int GetParentNotPerformedReasonId(int reasonId)
    {
        var reasonIds = new List<int>
        {
            Answers.EnvironmentalIssueAnswerId,
            Answers.InsufficientTrainingAnswerId,
            Answers.NoSuppliesOrEquipmentAnswerId,
            Answers.TechnicalIssueAnswerId,
            Answers.MemberPhysicallyUnableAnswerId
        };
        var kedReasonIds = new List<int>
        {
            Answers.KedEnvironmentalIssueAnswerId,
            Answers.KedInsufficientTrainingAnswerId,
            Answers.KedNoSuppliesOrEquipmentAnswerId,
            Answers.KedTechnicalIssueAnswerId,
            Answers.KedMemberPhysicallyUnableAnswerId
        };
        return reasonIds.Contains(reasonId)||kedReasonIds.Contains(reasonId)?Answers.ProviderReasonAnswerId:Answers.MemberReasonAnswerId;
        
    }
    
    protected async Task<ProviderPayRequestSent> GetProviderPayRequestSentEvent(int evaluationId)
    {
        try
        {
            return await CoreKafkaActions.GetUacrProviderPayRequestSentEvent<ProviderPayRequestSent>(evaluationId);
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
            return await CoreKafkaActions.GetUacrProviderPayableEventReceivedEvent<ProviderPayableEventReceived>(evaluationId);
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
            return await CoreKafkaActions.GetUacrProviderNonPayableEventReceivedEvent<ProviderNonPayableEventReceived>(evaluationId);
        }
        catch (KafkaEventsNotFoundException)
        {
            return null;
        }
    }

    protected static async Task<List<UacrNewRelicFilterEvent>> GetUacrFilterEvents(string eventId,
        string newRelicEvent)
        => await NewRelicActions.GetCustomEvent<UacrNewRelicFilterEvent>(newRelicEvent,
            new Dictionary<string, string> {{"EventId", $"'{eventId}'"}});

    protected static async Task<List<UacrNewRelicFilterEvent>> GetUacrFilterEventsByLabResultId(long labResultId,
        string newRelicEvent)
        => await NewRelicActions.GetCustomEvent<UacrNewRelicFilterEvent>(newRelicEvent,
            new Dictionary<string, string> {{"LabResultId", $"{labResultId}"}});
}