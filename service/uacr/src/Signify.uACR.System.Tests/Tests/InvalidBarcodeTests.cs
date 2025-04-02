using Signify.Dps.Test.Utilities.DataGen;
using Signify.EvaluationsApi.Core.Values;
using Signify.QE.Core.Exceptions;
using Signify.QE.MSTest.Attributes;
using Signify.uACR.Core.Events.Akka;
using Signify.uACR.System.Tests.Core.Constants;
using Signify.uACR.System.Tests.Core.Models.Kafka;
using UacrEvents;
using static Signify.Dps.Test.Utilities.Kafka.Constants.KafkaConstants;

namespace Signify.uACR.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class InvalidBarcodeTests : InvalidBarcodeActions
{
    public TestContext TestContext { get; set; }

    [RetryableTestMethod]
    [DataRow("barcode")]
    [DataRow("alphacode")]
    public async Task ANC_T1097_InValidBarCodesTests(string code)
    {
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();
        
        TestContext.WriteLine($"[{TestContext.TestName}]EvaluationID: " + evaluation.EvaluationId);
        
        var barCode = code.Equals("barcode")?GetInvalidBarcode():GetBarcode();
        var alphaCode = code.Equals("alphacode")?GetInvalidAlphaCode():GetAlphaCode();
        var answersDict = GeneratePerformedAnswers(barCode, alphaCode);
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        var eval = CoreApiActions.GetEvaluation(evaluation.EvaluationId);
        eval.Status.Should().Be("Completed");
        
        // Validate Exam Database record
        var exam = await GetExamByEvaluationId(evaluation.EvaluationId);
        exam.ExamId.Should().NotBe(null);

        // Validate BarCode
        var barcode = await GetBarcodeByExamId(exam.ExamId);
        barcode.Barcode.Should().Be(barCode + "|" + alphaCode);
        
        // Validate OrderCreation Kafka Event
        this.Invoking(async t =>
                await CoreKafkaActions.GetOrderCreationEvent<OrderCreationEvent>(evaluation.EvaluationId))
            .Should().ThrowAsync<KafkaEventsNotFoundException>().GetAwaiter().GetResult()
            .WithMessage($"Unable to find any consumed events from the {OmsOrderTopic} topic matching key: {evaluation.EvaluationId} value:  type: {OrderCreationEventType}");
        
        var labResultEvent = new UacrLabResult
        {
            EvaluationId = evaluation.EvaluationId,
            CreatinineResult = 1.07f,
            DateLabReceived = DateTime.Now.ToString("O"),
            UacrResult = "29",
            UrineAlbuminToCreatinineRatioResultColor = "Green",
            UrineAlbuminToCreatinineRatioResultDescription = "Performed"
        };
        
        // Publish LabResultReceived event to dps_labresult_uacr kafka topic
        LhaKafkaActions.PublishUacrLabResultEvent(labResultEvent, evaluation.EvaluationId.ToString());
        
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
        
        // Publish CdiPassed Kafka Event
        CoreKafkaActions.PublishCdiEvent(evaluation.EvaluationId, TestConstants.Product);
        
        // Assert
        var labResult = await GetLabResultByEvaluationId(evaluation.EvaluationId);
        labResult.ReceivedDate.Should().BeCloseTo(DateTime.Parse(labResultEvent.DateLabReceived), TimeSpan.FromSeconds(1));
        labResult.UacrResult.Should().Be(29);
        labResult.ResultColor.Should().Be("Green");
        labResult.ResultDescription.Should().Be("Performed");
        labResult.Normality.Should().Be("Normal");
        labResult.NormalityCode.Should().Be("N");

        // Validate result Kafka event
        var examResultEvent = await  CoreKafkaActions.GetUacrResultsReceivedEvent<ResultsReceived>(evaluation.EvaluationId);
        examResultEvent.Result.UacrResult.Should().Be(29);
        examResultEvent.Result.AbnormalIndicator.Should().Be("N");
        examResultEvent.Result.Description.Should().Be("Performed");
        examResultEvent.Determination.Should().Be("N");
        examResultEvent.IsBillable.Should().Be(true);
        examResultEvent.ProductCode.Should().Be(TestConstants.Product);
        examResultEvent.PerformedDate.Should().BeSameDateAs(DateTimeOffset.Parse(answersDict[Answers.DoSAnswerId]));

        var pdfDelivered = await GetPdfDeliveredByEvaluationId(evaluation.EvaluationId);
        pdfDelivered.BatchId.Should().Be(pdfDeliveryEvent.BatchId);
        pdfDelivered.BatchName.Should().Be(pdfDeliveryEvent.BatchName);
        pdfDelivered.EventId.Should().Be(pdfDeliveryEvent.EventId);

        var billingActions = new BillingActions();
        await billingActions.ValidateBillRequestSent(exam.ExamId, evaluation.EvaluationId, pdfDelivered,
            member.MemberPlanId);
        
        var providerPayActions = new ProviderPayActions();
        await providerPayActions.ValidateProviderPayRequestSent(exam.ExamId, evaluation.EvaluationId, member.MemberPlanId);
    }
    
}