using Signify.EvaluationsApi.Core.Values;
using Signify.QE.MSTest.Attributes;
using Signify.uACR.Core.Events.Akka;
using Signify.uACR.System.Tests.Core.Constants;
using Signify.uACR.System.Tests.Core.Models.Data;
using Signify.uACR.System.Tests.Core.Models.Kafka;

namespace Signify.uACR.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class LabResultTests : LabResultActions
{
    public TestContext TestContext { get; set; }

    [RetryableTestMethod]
    [DynamicData(nameof(GetNormalityData))]
    public async Task ANC_T788_LabResultTests(NormalityData normalityData)
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
        
        var eval = CoreApiActions.GetEvaluation(evaluation.EvaluationId);
        eval.Status.Should().Be("Completed");
        
        var labResultEvent = new UacrLabResult
        {
            EvaluationId = evaluation.EvaluationId,
            CreatinineResult = normalityData.CmpCreatinineResult,
            DateLabReceived = DateTime.Now.ToString("O"),
            UacrResult = normalityData.CmpUacrResult.ToString(),
            UrineAlbuminToCreatinineRatioResultColor = normalityData.CmpUacrResultColour,
            UrineAlbuminToCreatinineRatioResultDescription = "Performed"
        };
        
        LhaKafkaActions.PublishUacrLabResultEvent(labResultEvent, evaluation.EvaluationId.ToString());
        
        // Assert
        var labResult = await GetLabResultByEvaluationId(evaluation.EvaluationId);
        labResult.ReceivedDate.Should().BeCloseTo(DateTime.Parse(labResultEvent.DateLabReceived), TimeSpan.FromSeconds(1));
        labResult.UacrResult.Should().Be(normalityData.CmpUacrResult);
        labResult.ResultColor.Should().Be(normalityData.CmpUacrResultColour);
        labResult.ResultDescription.Should().Be("Performed");
        labResult.Normality.Should().Be(normalityData.Normality);
        labResult.NormalityCode.Should().Be(normalityData.NormalityIndicator);

        // Validate result Kafka event
        var examResultEvent = await  CoreKafkaActions.GetUacrResultsReceivedEvent<ResultsReceived>(evaluation.EvaluationId);
        examResultEvent.Result.UacrResult.Should().Be(normalityData.CmpUacrResult);
        examResultEvent.Result.AbnormalIndicator.Should().Be(normalityData.NormalityIndicator);
        examResultEvent.Result.Description.Should().Be("Performed");
        examResultEvent.Determination.Should().Be(normalityData.NormalityIndicator);
        examResultEvent.IsBillable.Should().Be(normalityData.IsBillable);
        examResultEvent.ProductCode.Should().Be(TestConstants.Product);
        examResultEvent.PerformedDate.Should().BeSameDateAs(DateTimeOffset.Parse(answersDict[Answers.DoSAnswerId]));
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