using Signify.EvaluationsApi.Core.Values;
using Signify.FOBT.Svc.System.Tests.Core.Constants;
using Signify.FOBT.Svc.System.Tests.Core.Actions;
using Signify.QE.MSTest.Attributes;
using Signify.QE.MSTest.Utilities;

namespace Signify.FOBT.Svc.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class SecondEvaluationTests: PerformedActions
{
    [RetryableTestMethod]
    public async Task ANC_T1012_DOS_scenario()
    {
        // Arrange
        var (member, appointment, evaluation) =
            await CoreApiActions.PrepareEvaluation();
        var answersDict = GeneratePerformedAnswers();

        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,
            CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);

        // Assert
        // Database FOBT table
        var fobt = await GetFOBTByEvaluationId(evaluation.EvaluationId, 20, 5);
        fobt.DateOfService.Should().Be(DateTime.Parse(answersDict[Answers.DosAnswerId]).Date);
        
        var newDateStamp = DateTime.Now.AddDays(2); 
        var newAnswers = GeneratePerformedAnswers();
        newAnswers[Answers.DosAnswerId] = newDateStamp.ToString("O");
        
        // Finalize evaluation the second time
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(newAnswers));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        Thread.Sleep(3000);
        
        // Assert
        // Database PAD table
        var doSrecord = await GetFOBTByEvaluationId(evaluation.EvaluationId, 15, 3);
        doSrecord.DateOfService.Should().Be(DateTime.Parse(newAnswers[Answers.DosAnswerId]).Date);
    }
}