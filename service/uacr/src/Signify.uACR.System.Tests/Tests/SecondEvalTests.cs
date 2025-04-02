using Signify.EvaluationsApi.Core.Values;
using Signify.QE.MSTest.Attributes;
using Signify.uACR.Core.Events.Akka;
using Signify.uACR.System.Tests.Core.Constants;

namespace Signify.uACR.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class SecondEvalTests : SecondEvalActions
{
    public TestContext TestContext { get; set; }

    [RetryableTestMethod]
    public async Task ANC_T1012_SecondEvaluation()
    {
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();
        
        TestContext.WriteLine($"[{TestContext.TestName}]EvaluationID: " + evaluation.EvaluationId);
        
        var barCode = GetBarcode();
        var alphaCode = GetAlphaCode();
        var answersDict = GeneratePerformedAnswers(barCode, alphaCode);
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        // Assert
        var finalizedEvent = await CoreKafkaActions.GetEvaluationEvent<EvaluationFinalizedEvent>(evaluation.EvaluationId, "EvaluationFinalizedEvent");
        
        var eval = CoreApiActions.GetEvaluation(evaluation.EvaluationId);
        eval.Status.Should().Be("Completed");
        
        // Validate Exam Database record
        var exam = await GetExamByEvaluationId(evaluation.EvaluationId);
        exam.DateOfService.Should().Be(finalizedEvent.DateOfService?.DateTime);

        // Second Evaluation with different DOS
        answersDict[Answers.DoSAnswerId] = DateTime.Now.AddDays(-2).ToString("O");
        
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        await Task.Delay(5000);
        
        // Validate Exam Database record
        var exam1 = await GetExamByEvaluationId(evaluation.EvaluationId);
        exam1.DateOfService.Date.Should().Be(DateTime.Parse(answersDict[Answers.DoSAnswerId]).Date);

    }
}