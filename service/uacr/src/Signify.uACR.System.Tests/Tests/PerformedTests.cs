using Signify.EvaluationsApi.Core.Values;
using Signify.QE.MSTest.Attributes;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Events.Akka;
using Signify.uACR.Core.Events.Status;
using Signify.uACR.System.Tests.Core.Constants;

namespace Signify.uACR.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class PerformedTests : PerformedActions
{
    public TestContext TestContext { get; set; }

    [RetryableTestMethod]
    public async Task ANC_T768_Performed()
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
        exam.EvaluationId.Should().Be(evaluation.EvaluationId);
        exam.ApplicationId.Should().Be(finalizedEvent.ApplicationId);
        exam.ProviderId.Should().Be(finalizedEvent.ProviderId);
        exam.MemberId.Should().Be(finalizedEvent.MemberId);
        exam.MemberPlanId.Should().Be(finalizedEvent.MemberPlanId);
        exam.CenseoId.Should().Be(member.CenseoId);
        exam.AppointmentId.Should().Be(finalizedEvent.AppointmentId);
        exam.ClientId.Should().Be(finalizedEvent.ClientId);
        exam.DateOfService.Should().Be(finalizedEvent.DateOfService?.DateTime);
        exam.FirstName.Should().Be(member.FirstName);
        exam.MiddleName.Should().Be(member.MiddleName);
        exam.LastName.Should().Be(member.LastName);
        exam.DateOfBirth.Date.Should().Be(member.DateOfBirth?.Date);
        exam.AddressLineOne.Should().Be(member.AddressLineOne);
        exam.AddressLineTwo.Should().Be(member.AddressLineTwo);
        exam.City.Should().Be(member.City);
        exam.State.Should().Be(member.State);
        exam.ZipCode.Should().Be(member.ZipCode);
        exam.NationalProviderIdentifier.Should().Be(Provider.NationalProviderIdentifier);
        DateTime.SpecifyKind(exam.EvaluationReceivedDateTime, DateTimeKind.Utc).Should().BeCloseTo(finalizedEvent.ReceivedDateTime.UtcDateTime,TimeSpan.FromSeconds(1));
        DateTime.SpecifyKind(exam.EvaluationCreatedDateTime, DateTimeKind.Utc).Should().BeCloseTo(finalizedEvent.CreatedDateTime.UtcDateTime,TimeSpan.FromSeconds(1));
        
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
        examStatusEvent.ProductCode.Should().Be(TestConstants.Product);
        examStatusEvent.ProviderId.Should().Be(Provider.ProviderId);
        examStatusEvent.MemberPlanId.Should().Be(member.MemberPlanId);
        examStatusEvent.CreatedDate.UtcDateTime.Should().Be(exam.EvaluationCreatedDateTime.ToUniversalTime());
        examStatusEvent.ReceivedDate.UtcDateTime.Should().Be(exam.EvaluationReceivedDateTime.ToUniversalTime());
    }
    
}