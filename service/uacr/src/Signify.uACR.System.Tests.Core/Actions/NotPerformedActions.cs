using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signify.uACR.Core.Constants;
using Signify.uACR.Core.Events.Status;
using Signify.uACR.System.Tests.Core.Constants;
using Signify.uACR.System.Tests.Core.Models.NewRelic;
using static Signify.uACR.System.Tests.Core.Constants.TestConstants;

namespace Signify.uACR.System.Tests.Core.Actions;

public class NotPerformedActions : BaseTestActions
{
    protected async Task Validate_NR_Event(int evaluationId, string reason)
    {
        var nREvent =
            await NewRelicActions.GetCustomEvent<EvaluationNotPerformedEvent>(Observability.Evaluation.EvaluationNotPerformedEvent, 
                new Dictionary<string, string>{{"EvaluationId", evaluationId.ToString()}, {"appName", "uacr"}});
        Assert.AreEqual(nREvent.First().NotPerformedReason, reason);
        
    }

    protected async Task ValidateKafkaEvent(int evaluationId, Dictionary<int,string> answersDict, int reasonAnswerId)
    {
        var reasonType = GetParentNotPerformedReason(reasonAnswerId);
        
        // Validate Exam Status Kafka Event
        if (Environment.GetEnvironmentVariable("TEST_ENV")!.Equals("prod"))
            return;
        var examStatusEvent = await CoreKafkaActions.GetUacrNotPerformedStatusEvent<NotPerformed>(evaluationId);
        examStatusEvent.ReasonType.Should().Be(reasonType);
        examStatusEvent.Reason.Should().Be(answersDict[reasonAnswerId]);
        examStatusEvent.ReasonNotes.Should().Be(Answers.ReasonNotesAnswer);
    }
}