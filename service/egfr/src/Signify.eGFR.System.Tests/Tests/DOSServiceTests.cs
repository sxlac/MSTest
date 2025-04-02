using Signify.eGFR.System.Tests.Core.Actions;
using Signify.EvaluationsApi.Core.Values;
using Signify.QE.MSTest.Attributes;

namespace Signify.eGFR.System.Tests.Tests;
using Signify.eGFR.System.Tests.Core.Constants;


[TestClass, TestCategory("regression")]
public class DOSServiceTests : DOSServiceActions
{
    public TestContext TestContext { get; set; }

    [RetryableTestMethod]
    public async Task ANC_T1012_DOS_Scenario()
    {
        // Arrange
        var (member, appointment, evaluation) =
            await CoreApiActions.PrepareEvaluation();
        var answersDict = GenerateKedPerformedAnswers();
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,CoreApiActions.GetEvaluationAnswerList(answersDict));

        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        CoreKafkaActions.PublishCdiEvent(evaluation.EvaluationId, Product, false);

        answersDict[Answers.DosAnswerId] = DateTime.UtcNow.AddDays(-1).ToString("O");
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        CoreKafkaActions.PublishCdiEvent(evaluation.EvaluationId, Product);
        
        var egfr = await GetExamByEvaluationId(evaluation.EvaluationId);
        egfr.DateOfService.Should().Be(DateTime.Parse(answersDict[Answers.DosAnswerId]).Date);
        
    }
}