using Newtonsoft.Json.Linq;
using Signify.EvaluationsApi.Core.Values;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.System.Tests.Core.Constants;

namespace Signify.PAD.Svc.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class BillingTests : BillingActions
{
    public TestContext TestContext { get; set; }
    
    [RetryableTestMethod]
    [DynamicData(nameof(GetNormalityTestData))]
    public async Task ANC_T728_BillingTest(string leftValue, string rightValue, string lNormality, string rNormality, string determination, string lSeverity, string rSeverity)
    {
        // Arrange
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();
        var answersDict = GeneratePerformedAnswers(leftValue,rightValue);
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);

        var pdfDeliveryEvent = new PdfDeliveredToClient()
        {
            BatchId = 12345,
            BatchName = "Pad_System_Tests" + DateTime.Now.Date.ToString("yyyyMMdd"),
            ProductCodes = new List<string>{TestConstants.Product},
            CreatedDateTime = DateTime.Now,
            DeliveryDateTime = DateTime.Now,
            EvaluationId = evaluation.EvaluationId,
            EventId = Guid.NewGuid()
        };
        
        // Publish PdfDeliveredToClient event to pdfdelivery kafka topic
        CoreKafkaActions.PublishPdfDeliveryEvent(pdfDeliveryEvent,evaluation.EvaluationId.ToString());
        
        var exam = await GetPadByEvaluationId(evaluation.EvaluationId, 15, 3);
        if (determination == "U")
        {
            await ValidateBillRequestNotSent(evaluation.EvaluationId, exam.PADId, member.MemberPlanId);
        }
        else
        {
            await ValidateBillRequestSent(pdfDeliveryEvent, evaluation.EvaluationId, exam.PADId, member.MemberPlanId);
        }
    }
    
    public static IEnumerable<object[]> GetNormalityTestData
    {
        get{
            const string filePath = "../../../../Signify.PAD.Svc.System.Tests.Core/Data/normality.json";

            var jArray = JArray.Parse(File.ReadAllText(filePath));

            foreach (var normalityData in jArray)
            {
                if ((string)normalityData["determination"] == "U")
                    continue; // remove this when ANC-7020 is done
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