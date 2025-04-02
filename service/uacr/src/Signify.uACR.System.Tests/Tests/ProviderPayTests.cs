using Signify.EvaluationsApi.Core.Values;
using Signify.QE.MSTest.Attributes;
using Signify.uACR.Core.Events.Akka;
using Signify.uACR.System.Tests.Core.Constants;
using Signify.uACR.System.Tests.Core.Models.Data;
using Signify.uACR.System.Tests.Core.Models.Kafka;

namespace Signify.uACR.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class ProviderPayTests : ProviderPayActions
{
    public TestContext TestContext { get; set; }
    
    [RetryableTestMethod]
    [DynamicData(nameof(GetNormalityData))]
    public async Task ANC_T759_ProviderPayTests(NormalityData normalityData)
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
        
        var finalizedEvent = await CoreKafkaActions.GetEvaluationEvent<EvaluationFinalizedEvent>(evaluation.EvaluationId, "EvaluationFinalizedEvent");
        
        var eval = CoreApiActions.GetEvaluation(evaluation.EvaluationId);
        eval.Status.Should().Be("Completed");
        
        var exam = await GetExamByEvaluationId(evaluation.EvaluationId);
        
        var labResultEvent = new UacrLabResult
        {
            EvaluationId = evaluation.EvaluationId,
            CreatinineResult = normalityData.CmpCreatinineResult,
            DateLabReceived = DateTime.Now.ToString("O"),
            UacrResult = normalityData.CmpUacrResult.ToString(),
            UrineAlbuminToCreatinineRatioResultColor = normalityData.CmpUacrResultColour,
            UrineAlbuminToCreatinineRatioResultDescription = "Performed"
        };
        
        // Publish LabResultReceived event to dps_labresult_uacr kafka topic
        LhaKafkaActions.PublishUacrLabResultEvent(labResultEvent, evaluation.EvaluationId.ToString());
        
        // Publish CdiPassed Kafka Event
        CoreKafkaActions.PublishCdiEvent(evaluation.EvaluationId, TestConstants.Product);

        if (normalityData.NormalityIndicator.Equals("U"))
        {
            await ValidateProviderPayRequestNotSent(exam.ExamId, evaluation.EvaluationId, member.MemberPlanId);
        }
        else
        {
            await ValidateProviderPayRequestSent(exam.ExamId, evaluation.EvaluationId, member.MemberPlanId);
        }
    }
    
    #region TestData

    public static IEnumerable<object[]> GetNormalityData
    {
        get
        {
            return new[]
            {
                new object[]
                {
                    new NormalityData
                    {
                        Normality = "Normal",
                        CmpUacrResult = 29,
                        CmpUacrResultColour = "Green",
                        CmpCreatinineResult = 1.07f,
                        IsBillable = true,
                        NormalityIndicator = "N"
                    }

                },
                [
                    new NormalityData
                    {
                        Normality = "Abnormal",
                        CmpUacrResult = 30,
                        CmpUacrResultColour = "Red",
                        CmpCreatinineResult = 1.27f,
                        IsBillable = true,
                        NormalityIndicator = "A"
                    } 
                ],
                [
                    new NormalityData
                    {
                        Normality = "Abnormal",
                        CmpUacrResult = 31,
                        CmpUacrResultColour = "Red",
                        CmpCreatinineResult = 1.27f,
                        IsBillable = true,
                        NormalityIndicator = "A"
                    }
                ],
                [
                    new NormalityData
                    {
                        Normality = "Undetermined",
                        CmpUacrResult = 0,
                        CmpUacrResultColour = "Grey",
                        CmpCreatinineResult = 1.27f,
                        IsBillable = false,
                        NormalityIndicator = "U"
                    }
                ]
            };
        }
    }

    #endregion
}