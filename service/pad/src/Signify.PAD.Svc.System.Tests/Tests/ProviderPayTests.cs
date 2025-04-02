using Newtonsoft.Json.Linq;
using Signify.EvaluationsApi.Core.Values;

namespace Signify.PAD.Svc.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class ProviderPayTests : ProviderPayActions
{
    
    [RetryableTestMethod]
    [DynamicData(nameof(GetNormalityTestData))]
    public async Task ANC_T571_ProviderPayTest(string leftValue, string rightValue, string lNormality, string rNormality, string determination, string lSeverity, string rSeverity)
    {
        // Arrange
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();
        var answersDict = GeneratePerformedAnswers(leftValue,rightValue);


        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);

        if (determination == "U")
        {
            await ValidateProviderNonPayable(evaluation.EvaluationId);
        }
        else
        {
            await ValidateProviderPayable(evaluation.EvaluationId, member.MemberPlanId);
        }
    }
    
    public static IEnumerable<object[]> GetNormalityTestData
    {
        get{
            const string filePath = "../../../../Signify.PAD.Svc.System.Tests.Core/Data/normality.json";

            var jArray = JArray.Parse(File.ReadAllText(filePath));

            foreach (var normalityData in jArray)
            {
                yield return [(string)normalityData["leftValue"], 
                    (string)normalityData["rightValue"], 
                    (string)normalityData["leftNormality"], 
                    (string)normalityData["rightNormality"], 
                    (string)normalityData["determination"], 
                    (string)normalityData["lSeverity"], 
                    (string)normalityData["rSeverity"]];
            }
        }
    }
}