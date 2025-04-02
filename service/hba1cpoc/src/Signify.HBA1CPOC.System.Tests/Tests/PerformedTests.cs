using CH.Evaluation.Events;
using Signify.QE.MSTest.Attributes;
using Signify.EvaluationsApi.Core.Values;
using Signify.HBA1CPOC.Messages.Events.Status;

namespace Signify.HBA1CPOC.System.Tests.Tests;



[TestClass,TestCategory("regression")]
public class PerformedTests: PerformedActions
{
    public TestContext TestContext { get; set; }

    [RetryableTestMethod]
    [DataRow("3.9", "A")]
    [DataRow("4", "N")]
    [DataRow("6.9", "N")]
    public async Task ANC_T324_Hba1cpocPerformed(string answerValue, string normality)
    {
        // Arrange
        var (member,appointment,evaluation) = await CoreApiActions.PrepareEvaluation();
        
        var answers = GeneratePerformedAnswers(percentA1C:answerValue);
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answers));
         
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        var  dateStamp = DateTime.UtcNow; 
        // Assert
        var evaluationEvents = await CoreKafkaActions.GetEvaluationEvent<EvaluationFinalizedEvent>(evaluation.EvaluationId, 
            "EvaluationFinalizedEvent");
        
        var eval = CoreApiActions.GetEvaluation(evaluation.EvaluationId);
        eval.Status.Should().Be("Completed");
        
        // Validate the entry using EvaluationId in HBA1CPOC & HBA1CPOCStatus tables. Status 1 = HBA1CPOCPerformed 
        var performedRecord = GetHba1CpocRecordByEvaluationId(evaluation.EvaluationId);
        performedRecord.EvaluationId.Should().Be(evaluation.EvaluationId);
        performedRecord.MemberPlanId.Should().Be(member.MemberPlanId);
        performedRecord.CenseoId.Should().Be(member.CenseoId);
        performedRecord.AppointmentId.Should().Be(appointment.AppointmentId);
        performedRecord.ProviderId.Should().Be(Provider.ProviderId);
        performedRecord.A1CPercent.Should().Be(answerValue);
        performedRecord.NormalityIndicator.Should().Be(normality);
        performedRecord.ReceivedDateTime.Should().BeSameDateAs(dateStamp);
        //performedRecord.DateOfBirth.Should().Be(member.DateDateOfBirth);
        //performedRecord.ExpirationDate.ToString().Should().Be(expirationDate.ToString());
        
        ValidateHBA1CPOCStatusCodeByHBA1CPOCId(performedRecord.HBA1CPOCId, 1);
        
        //  Validate that the Kafka events include the expected event headers
        var performedEvent = await CoreKafkaActions.GetA1CpocPerformedStatusEvent<Performed>(evaluation.EvaluationId);
        performedEvent.MemberPlanId.Should().Be(member.MemberPlanId); 
        performedEvent.ProductCode.Should().Be("HBA1CPOC");
    }
}