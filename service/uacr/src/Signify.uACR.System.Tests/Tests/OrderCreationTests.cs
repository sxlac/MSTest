using Signify.Dps.Test.Utilities.Database.Exceptions;
using Signify.EvaluationsApi.Core.Values;
using Signify.QE.Core.Exceptions;
using Signify.QE.MSTest.Attributes;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Events.Akka;
using Signify.uACR.Core.Events.Status;
using Signify.uACR.System.Tests.Core.Constants;
using static Signify.Dps.Test.Utilities.Kafka.Constants.KafkaConstants;

namespace Signify.uACR.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class OrderCreationTests : OrderCreationActions
{
    public TestContext TestContext { get; set; }
    
    [RetryableTestMethod]
    public async Task ANC_T768_OrderCreation()
    {
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();
        
        TestContext.WriteLine($"[{TestContext.TestName}]EvaluationID: " + evaluation.EvaluationId);
        
        var barCode = GetBarcode();
        var alphaCode = GetAlphaCode();
        var answersDict = GeneratePerformedAnswers(barCode, alphaCode);
        
        var answers = CoreApiActions.GetEvaluationAnswerList(answersDict);
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,answers);
        
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        // Assert
        var finalizedEvent = await CoreKafkaActions.GetEvaluationEvent<EvaluationFinalizedEvent>(evaluation.EvaluationId, "EvaluationFinalizedEvent");
        
        var eval = CoreApiActions.GetEvaluation(evaluation.EvaluationId);
        eval.Status.Should().Be("Completed");
        
        // Validate Exam Database record
        var exam = await GetExamByEvaluationId(evaluation.EvaluationId);
        exam.ExamId.Should().NotBe(null);
        
        // Validate ExamStatuses in DB
        var expectedStatusCodes = new List<int>
        {
            ExamStatusCode.ExamPerformed.ExamStatusCodeId,
            ExamStatusCode.OrderRequested.ExamStatusCodeId
        };
        await ValidateExamStatusCodesByEvaluationId(evaluation.EvaluationId, expectedStatusCodes);

        // Validate BarCode Database record
        var barcodeExam = await GetBarcodeByExamId(exam.ExamId);
        barcodeExam.Barcode.Should().Be(barCode+"|"+alphaCode);
        
        // Validate Exam Status Kafka Event
        var examStatusEvent = await CoreKafkaActions.GetUacrPerformedStatusEvent<Performed>(evaluation.EvaluationId);
        examStatusEvent.Barcode.Should().Be(barcodeExam.Barcode);
        
        // Validate OrderCreation Kafka Event
        var orderCreationEvent =
            await CoreKafkaActions.GetOrderCreationEvent<OrderCreationEvent>(evaluation.EvaluationId);
        orderCreationEvent.ProductCode.Should().Be(TestConstants.Product);
        orderCreationEvent.Vendor.Should().Be(Vendor);
        orderCreationEvent.Context["LgcBarcode"].Should().Be(barCode);
        orderCreationEvent.Context["LgcAlphaCode"].Should().Be(alphaCode);
    }
    
    [RetryableTestMethod]
    [DataRow("barcode")]
    [DataRow("alphacode")]
    public async Task ANC_T793_OrderCreation_InvalidBarcodes(string code)
    {
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();
        
        TestContext.WriteLine($"[{TestContext.TestName}]EvaluationID: " + evaluation.EvaluationId);
        
        var barCode = code.Equals("barcode")?GetInvalidBarcode():GetBarcode();
        var alphaCode = code.Equals("alphacode")?GetInvalidAlphaCode():GetAlphaCode();
        var answersDict = GeneratePerformedAnswers(barCode, alphaCode);
        
        var answers = CoreApiActions.GetEvaluationAnswerList(answersDict);
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,answers);
        
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        // Assert
        var finalizedEvent = await CoreKafkaActions.GetEvaluationEvent<EvaluationFinalizedEvent>(evaluation.EvaluationId, "EvaluationFinalizedEvent");
        
        var eval = CoreApiActions.GetEvaluation(evaluation.EvaluationId);
        eval.Status.Should().Be("Completed");
        
        // Validate Exam Database record
        var exam = await GetExamByEvaluationId(evaluation.EvaluationId);
        exam.ExamId.Should().NotBe(null);
        
        // Validate BarCode Database record
        var barcodeExam = await GetBarcodeByExamId(exam.ExamId);
        barcodeExam.Barcode.Should().Be(barCode+"|"+alphaCode);
        
        // Validate Exam Status Kafka Event
        var examStatusEvent = await CoreKafkaActions.GetUacrPerformedStatusEvent<Performed>(evaluation.EvaluationId);
        examStatusEvent.Barcode.Should().Be(barcodeExam.Barcode);
        
        // Validate ExamStatuses in DB
        var expectedStatusCodes = new List<int>
        {
            ExamStatusCode.OrderRequested.ExamStatusCodeId
        }; 
        
        this.Invoking(async t=> await ValidateExamStatusCodesByExamId(exam.ExamId, expectedStatusCodes))
            .Should().ThrowAsync<ExamStatusCodeNotFoundException>().GetAwaiter().GetResult()
            .WithMessage($"ExamStatusCodeIds {string.Join(", ",expectedStatusCodes)} not found for ExamId {exam.ExamId}");


        // Validate OrderCreation Kafka Event
        this.Invoking(async t =>
            await CoreKafkaActions.GetOrderCreationEvent<OrderCreationEvent>(evaluation.EvaluationId))
            .Should().ThrowAsync<KafkaEventsNotFoundException>().GetAwaiter().GetResult()
            .WithMessage($"Unable to find any consumed events from the {OmsOrderTopic} topic matching key: {evaluation.EvaluationId} value:  type: {OrderCreationEventType}");
    }
}