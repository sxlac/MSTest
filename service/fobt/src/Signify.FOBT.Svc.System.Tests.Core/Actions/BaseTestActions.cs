using Signify.Dps.Test.Utilities.CoreApi.Actions;
using Signify.Dps.Test.Utilities.Kafka.Actions;
using Signify.Dps.Test.Utilities.DataGen;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.System.Tests.Core.Constants;
using Signify.FOBT.Svc.System.Tests.Core.Models.Kafka;
using Signify.QE.Core.Actions;
using Signify.QE.Core.Exceptions;

namespace Signify.FOBT.Svc.System.Tests.Core.Actions;

public class BaseTestActions : DatabaseActions
{
    protected CoreApiActions CoreApiActions = new (CoreApiConfigs, Provider.ProviderId, TestConstants.Product, FormVersionId, LoggingHttpMessageHandler);
    protected CoreKafkaActions CoreKafkaActions = new CoreKafkaActions(KafkaConfig, KafkaProducerConfig, TestConstants.Product)
        .AddStatusAndResultsTopics(StatusTopic, ResultsTopic); 
    protected CoreKafkaActions CoreHomeAccessKafkaActions = new CoreKafkaActions(KafkaConfig, KafkaHaProducerConfig, TestConstants.Product)
        .AddStatusAndResultsTopics(StatusTopic, ResultsTopic);
    
    
    protected static Dictionary<int, string> GenerateNotPerformedAnswers()
    {
        return new Dictionary<int, string>
        {
            { Answers.UnableToPerformNotesAnswerId, "Unable to perform notes." },
            { Answers.MemberRefusalNotesAnswerId, "Member refusal notes." },
            { Answers.DosAnswerId, DateTime.Now.ToString("O") }
        };
    }

    protected async Task<bool> ValidateExamStatusCodesByEvaluationId(int evaluationId, List<int> expectedIds, int retryCount, int waitSeconds)
    {
        var exam = await GetFOBTByEvaluationId(evaluationId, retryCount, waitSeconds);
        return await ValidateExamStatusCodesByExamId(exam.FOBTId, expectedIds, 5, 3);
    }
    protected static Dictionary<int, string> GeneratePerformedAnswers()
    {
        var barcode = DataGen.RandomInt(100000, 999999).ToString();
        return new Dictionary<int, string>
        {
            { Answers.PerformedYesAnswerId, "Yes" },
            { Answers.Barcode, barcode},
            { Answers.DosAnswerId, DateTime.Now.ToString("O") }
        };
    }
    protected async Task<ProviderPayRequestSent> GetProviderPayRequestSentEvent(int evaluationId)
    {
        try
        {
            return await CoreKafkaActions.GetProviderPayRequestSentEvent<ProviderPayRequestSent>(evaluationId);
        }
        catch (KafkaEventsNotFoundException nex)
        {
            return null;
        }
    }
    protected async Task<ProviderPayableEventReceived> GetProviderPayableEventReceivedEvent(int evaluationId)
    {
        try
        {
            return await CoreKafkaActions.GetProviderPayableEventReceivedEvent<ProviderPayableEventReceived>(evaluationId);
        }
        catch (KafkaEventsNotFoundException nex)
        {
            return null;
        }
    }
    protected async Task<ProviderNonPayableEventReceived> GetProviderNonPayableEventReceivedEvent(int evaluationId)
    {   
        try
        {
            return await CoreKafkaActions.GetProviderNonPayableEventReceivedEvent<ProviderNonPayableEventReceived>(evaluationId);
        }
        catch (KafkaEventsNotFoundException nex)
        {
            return null;
        }
    }
    
}