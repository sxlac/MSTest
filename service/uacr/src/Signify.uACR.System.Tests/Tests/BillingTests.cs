using Signify.Dps.Test.Utilities.DataGen;
using Signify.EvaluationsApi.Core.Values;
using Signify.QE.MSTest.Attributes;
using Signify.uACR.Core.Events.Akka;
using Signify.uACR.System.Tests.Core.Constants;
using Signify.uACR.System.Tests.Core.Models.Data;
using Signify.uACR.System.Tests.Core.Models.Kafka;
using UacrEvents;

namespace Signify.uACR.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class BillingTests : BillingActions
{
    public TestContext TestContext { get; set; }

    [RetryableTestMethod]
    [DynamicData(nameof(GetNormalityData))]
    public async Task ANC_T792_BillingTests(NormalityData normalityData)
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
        
        var pdfDeliveryEvent = new PdfDeliveredToClient
        {
            BatchId = 12345,
            BatchName = "Uacr_System_Tests" + DateTime.Now.Date.ToString("yyyyMMdd"),
            ProductCodes = new List<string>{TestConstants.Product},
            CreatedDateTime = DateTime.Now,
            DeliveryDateTime = DateTime.Now,
            EvaluationId = evaluation.EvaluationId,
            EventId = DataGen.NewGuid()
        };
        
        // Publish PdfDeliveredToClient event to pdfdelivery kafka topic
        CoreKafkaActions.PublishPdfDeliveryEvent(pdfDeliveryEvent,evaluation.EvaluationId.ToString());
        
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
        
        var pdfDelivered = await GetPdfDeliveredByEvaluationId(evaluation.EvaluationId);
        pdfDelivered.BatchId.Should().Be(pdfDeliveryEvent.BatchId);
        pdfDelivered.BatchName.Should().Be(pdfDeliveryEvent.BatchName);
        pdfDelivered.EventId.Should().Be(pdfDeliveryEvent.EventId);

        if (normalityData.NormalityIndicator.Equals("U"))
        {
            await ValidateBillRequestNotSent(exam.ExamId, evaluation.EvaluationId, pdfDelivered, member.MemberPlanId);
        }
        else
        {
            await ValidateBillRequestSent(exam.ExamId, evaluation.EvaluationId, pdfDelivered, member.MemberPlanId);
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