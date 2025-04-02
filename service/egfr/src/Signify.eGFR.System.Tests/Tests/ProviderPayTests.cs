using Signify.EvaluationsApi.Core.Values;
using Signify.eGFR.System.Tests.Core.Actions;
using Signify.QE.MSTest.Attributes;
using Signify.eGFR.System.Tests.Core.Models.Kafka;

namespace Signify.eGFR.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class ProviderPayTests : ProviderPayActions
{
    public TestContext TestContext { get; set; }
    
    [RetryableTestMethod(2)]
    [DataRow("65", "N")]
    [DataRow("59", "A")]
    [DataRow("", "U")]
    public async Task ANC_T628_ProviderPayTest(string egfrResult, string normality)
    {
        // Arrange
        var (member, appointment, evaluation) =
            await CoreApiActions.PrepareEvaluation();
        var answersDict = GenerateKedPerformedAnswers();


        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        // Database eGFR table
        var egfr = await GetExamByEvaluationId(evaluation.EvaluationId);

        // Publish the homeaccess lab results
        var resultsReceivedValue = new HomeAccessLabResults()
        {
            EstimatedGlomerularFiltrationRateResultColor = "",
            EstimatedGlomerularFiltrationRateResultDescription = "abcd",
            EgfrResult = egfrResult,
            EvaluationId = evaluation.EvaluationId,
            DateLabReceived = DateTime.UtcNow,
        };
        CoreHomeAccessKafkaActions.PublishEgfrLabResultEvent(resultsReceivedValue,evaluation.EvaluationId.ToString());
        await Task.Delay(5000);

        if (normality == "U") 
        {
            await ValidateProviderNonPayable(evaluation.EvaluationId);
        }
        else
        {
            await ValidateProviderPayable(evaluation.EvaluationId, member.MemberPlanId);
        }
    }
}