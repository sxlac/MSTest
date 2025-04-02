using Signify.Dps.Test.Utilities.CoreApi.Actions;
using Signify.Dps.Test.Utilities.FileShare.Actions;
using Signify.Dps.Test.Utilities.Kafka.Actions;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.Core.Models;
using Signify.PAD.Svc.System.Tests.Core.Constants;
using Signify.QE.Core.Exceptions;

namespace Signify.PAD.Svc.System.Tests.Core.Actions;

public class BaseTestActions : DatabaseActions
{
    protected FileShareActions FileShareActions { get; } = new();
    protected CoreApiActions CoreApiActions { get; } = new (CoreApiConfigs, Provider.ProviderId, TestConstants.Product, FormVersionId, LoggingHttpMessageHandler);
    protected CoreKafkaActions CoreKafkaActions { get; } = new CoreKafkaActions(KafkaConfig, KafkaProducerConfig, TestConstants.Product)
                                                    .AddStatusAndResultsTopics(StatusTopic, ResultsTopic, ClinicalTopic);
    
    protected Dictionary<int, string> GeneratePerformedAnswers(string leftResult=null, string rightResult=null, DateTime? dos=null)
    {
        var lResult = leftResult ?? "1";
        var rResult = rightResult ?? "1";
        var date = dos ?? DateTime.Now;
        return new Dictionary<int, string>
        {
            { Answers.PerformedYesAnswerId, "Yes" },
            { Answers.LeftResultAnswerId, lResult },
            { Answers.RightResultAnswerId, rResult },
            { Answers.DosAnswerId, date.ToString("O") }
        };
    }
    
    protected static Dictionary<int, string> GeneratePerformedAnswersWithSeverity(
        int leftAnswerResult, int rightAnswerResult,
        string leftResult=null, string rightResult=null,
        string lSeverity=null, string rSeverity=null
        )
    {
        var lResult = leftResult ?? "1";
        var rResult = rightResult ?? "1";
        var leftSeverity = lSeverity ?? "Normal";
        var rightSeverity = rSeverity ?? "Normal";
        var date = DateTime.Now;
        return new Dictionary<int, string>
        {
            { Answers.PerformedYesAnswerId, "Yes" },
            { Answers.LeftResultAnswerId, lResult },
            { Answers.RightResultAnswerId, rResult },
            { Answers.DosAnswerId, date.ToString("0") },
            { leftAnswerResult, leftSeverity },
            { rightAnswerResult, rightSeverity }
            
        };
    }

    protected static Dictionary<int, string> GenerateNotPerformedAnswers()
    {
        return new Dictionary<int, string>
        {
            { Answers.PerformedNoAnswerId, "No" },
            { Answers.UnableToPerformAnswerId, "Yes" },
            { Answers.TechnicalIssueAnswerId, "Technical issue" },
            { Answers.UnableToPerformNotesAnswerId, "Unable to perform notes." },
            { Answers.DosAnswerId, DateTime.Now.ToString("O") }
        };
    }

    protected async Task ValidateExamStatusCodesByEvaluationId(int evaluationId, List<int> expectedIds, int retryCount, int waitSeconds)
    {
        var exam = await GetPadByEvaluationId(evaluationId, retryCount, waitSeconds);
        Assert.IsTrue(await ValidateExamStatusCodesByExamId(exam.PADId, expectedIds, 5, 3));
    }

    protected async Task<CDIPassedEvent> GetCdiPassedEvent(int evaluationId)
    {
        return await CoreKafkaActions.GetCdiEvent<CDIPassedEvent>(evaluationId);
    }
    
    protected async Task<CDIFailedEvent> GetCdiFailedEvent(int evaluationId)
    {
        return await CoreKafkaActions.GetCdiEvent<CDIFailedEvent>(evaluationId, false);
    }
    
    protected async Task<ProviderPayRequestSent> GetProviderPayRequestSentEvent(int evaluationId)
    {
        try
        {
            return await CoreKafkaActions.GetProviderPayRequestSentEvent<ProviderPayRequestSent>(evaluationId);
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
            return await CoreKafkaActions.GetProviderPayableEventReceivedEvent<ProviderPayableEventReceived>(evaluationId);
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
            return await CoreKafkaActions.GetProviderNonPayableEventReceivedEvent<ProviderNonPayableEventReceived>(evaluationId);
        }
        catch (KafkaEventsNotFoundException)
        {
            return null;
        }
    }
}