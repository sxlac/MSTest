using Signify.EvaluationsApi.Core.Values;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.System.Tests.Core.Constants;

namespace Signify.PAD.Svc.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class DoSTests : PerformedActions
{
    
    [RetryableTestMethod]
    public async Task ANC_T1012_DOS_scenario()
    {
        // Arrange
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();
        var answersDict = GeneratePerformedAnswers();
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        // Assert
        // Database PAD table
        var pad = await GetPadByEvaluationId(evaluation.EvaluationId, 15, 3);
        pad.DateOfService.Should().Be(DateTime.Parse(answersDict[Answers.DosAnswerId]).Date);
        
        var finalizedEvent =
            await CoreKafkaActions.GetEvaluationEvent<EvaluationFinalizedEvent>(evaluation.EvaluationId,
                "EvaluationFinalizedEvent");
        
        var newDateStamp = DateTime.Now.AddDays(2); 
        var newAnswers = GeneratePerformedAnswers();
        newAnswers[Answers.DosAnswerId] = newDateStamp.ToString("O");
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(newAnswers));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        Thread.Sleep(3000);
        
        // Assert
        // Database PAD table
        var doSrecord = await GetPadByEvaluationId(evaluation.EvaluationId, 15, 3);
        doSrecord.DateOfService.Should().Be(DateTime.Parse(newAnswers[Answers.DosAnswerId]).Date);
        
    }
}