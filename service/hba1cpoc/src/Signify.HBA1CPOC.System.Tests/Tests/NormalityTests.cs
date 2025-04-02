using Signify.QE.MSTest.Attributes;
using Signify.EvaluationsApi.Core.Values;
using Signify.HBA1CPOC.Messages.Events.Status;

namespace Signify.HBA1CPOC.System.Tests.Tests;

[TestClass,TestCategory("regression")]
public class NormalityTests: NormalityActions
{
    public TestContext TestContext { get; set; }
    
    [RetryableTestMethod]
    [DataRow("3.9", "A")]
    [DataRow("4", "N")]
    [DataRow("6.9", "N")]
    [DataRow("7", "A")] 
    [DataRow("abc", "U")]
    [DataRow("<4", "A")]
    [DataRow(">13", "A")]
    [DataRow("<4%", "A")]
    [DataRow("<4.0", "A")]
    [DataRow(">13.0", "A")]
    public async Task ANC_T327_Hba1cpocNormality(string answerValue, string normality)
    {
        // Arrange
        var (member,appointment,evaluation) = await CoreApiActions.PrepareEvaluation();
        var expirationDate = DateTime.Now;
        var answers = GeneratePerformedAnswers(percentA1C:answerValue);
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answers));
         
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        var  dateStamp = DateTime.UtcNow; 
        // Assert
        var eval = CoreApiActions.GetEvaluation(evaluation.EvaluationId);
        eval.Status.Should().Be("Completed");
        
        // Validate the entry using EvaluationId in HBA1CPOC & HBA1CPOCStatus tables. Status 1 = HBA1CPOCPerformed 
        var normalityRecord = GetHba1CpocRecordByEvaluationId(evaluation.EvaluationId);
        normalityRecord.EvaluationId.Should().Be(evaluation.EvaluationId);
        normalityRecord.MemberPlanId.Should().Be(member.MemberPlanId);
        normalityRecord.CenseoId.Should().Be(member.CenseoId);
        normalityRecord.AppointmentId.Should().Be(appointment.AppointmentId);
        normalityRecord.ProviderId.Should().Be(Provider.ProviderId);
        normalityRecord.A1CPercent.Should().Be(answerValue);
        normalityRecord.NormalityIndicator.Should().Be(normality);
        normalityRecord.ReceivedDateTime.Should().BeSameDateAs(dateStamp);
        member.DateOfBirth.Should().BeSameDateAs(normalityRecord.DateOfBirth);
        normalityRecord.ExpirationDate.Should().BeSameDateAs(expirationDate);
        
        ValidateHBA1CPOCStatusCodeByHBA1CPOCId(normalityRecord.HBA1CPOCId, 1);
        
        //  Validate that the Kafka events include the expected event headers
        var normalityEvent = await CoreKafkaActions.GetA1CpocPerformedStatusEvent<Performed>(evaluation.EvaluationId);
        normalityEvent.MemberPlanId.Should().Be(member.MemberPlanId); 
        normalityEvent.ProductCode.Should().Be("HBA1CPOC");
        normalityEvent.EvaluationId.Should().Be(evaluation.EvaluationId);
        normalityEvent.ProviderId.Should().Be(Provider.ProviderId);
    }
}