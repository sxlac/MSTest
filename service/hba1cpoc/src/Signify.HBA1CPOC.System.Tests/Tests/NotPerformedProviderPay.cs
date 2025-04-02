using Signify.EvaluationsApi.Core.Models;
using Signify.QE.MSTest.Attributes;
using Signify.EvaluationsApi.Core.Values;
using Signify.HBA1CPOC.Messages.Events.Status;
using Signify.HBA1CPOC.System.Tests.Core.Constants;

namespace Signify.HBA1CPOC.System.Tests.Tests;


[TestClass, TestCategory("regression")]
public class NotPerformedProviderPay: NotPerformedActions
{
    public TestContext TestContext { get; set; }
    private readonly ProviderPayActions _providerPayActions = new ();
    
    [RetryableTestMethod]
    [DataRow (33088,"Member refused",33074,"Member recently completed", 33071,"2", 33079)]
    public async Task ANC_T601_HBA1CPOC_Non_Payable_Not_Performed_Exam(
        int notPerformedAnswerId, string reason,
        int reasonId, string reasonNote,
        int noAnswerId, string noValue,
        int notesAnswerId)
    {
        
        // Arrange
        var (member,appointment,evaluation) = await CoreApiActions.PrepareEvaluation();
        var randomNote = DateTime.Now.Date.ToString();
        var answers = new List<EvaluationAnswer>
            
        {
            new() { AnswerId = noAnswerId, AnswerValue = noValue},
            new() { AnswerId = notPerformedAnswerId, AnswerValue = reason },
            new() { AnswerId = reasonId, AnswerValue = reasonNote },
            new() { AnswerId = notesAnswerId, AnswerValue = randomNote },
            
        };
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,answers);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        // Assert
        var eval = CoreApiActions.GetEvaluation(evaluation.EvaluationId);
        eval.Status.Should().Be("Completed");
        
        // Validate that the database HBA1CPOC details are as expected using EvaluationId in HBA1CPOC
        var notPerformedReason = GetNotPerformedReasonByEvaluationId(evaluation.EvaluationId);
        notPerformedReason.AnswerId.Should().Be(reasonId);
        notPerformedReason.Reason.Should().Be(reasonNote);
        var notPerformed = GetNotPerformedByEvaluationId(evaluation.EvaluationId);
        // Validate the entry using EvaluationId in HBA1CPOC & HBA1CPOCStatus tables
        var expectedStatusCodes = new List<int>
        {
            HBA1CPOCStatusCodes.HBA1CPOCNotPerformed.HBA1CPOCStatusCodeId,
            HBA1CPOCStatusCodes.BillRequestNotSent.HBA1CPOCStatusCodeId, 
        };
        ValidateExamStatusCodesByExamId(notPerformed.HBA1CPOCId,expectedStatusCodes);
        
        // # Validate that the Kafka events include the expected event headers
        var examStatusEvent = await CoreKafkaActions.GetA1CpocNotPerformedStatusEvent<NotPerformed>(evaluation.EvaluationId);
        examStatusEvent.Reason.Should().Be(reasonNote);
        examStatusEvent.ReasonType.Should().Be(reason);
        examStatusEvent.ReasonNotes.Should().Be(randomNote);
        examStatusEvent.EvaluationId.Should().Be(evaluation.EvaluationId);
        examStatusEvent.MemberPlanId.Should().Be(member.MemberPlanId);
        examStatusEvent.ProductCode.Should().Be("HBA1CPOC");
       
        //  Validate that a Kafka event - ProviderPayRequestSent - was not raised
        (await _providerPayActions.GetProviderPayRequestSentEvent(evaluation.EvaluationId)).Should().BeNull();
        //  Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        (await _providerPayActions.GetProviderPayableEventReceivedEvent(evaluation.EvaluationId)).Should().BeNull();
        (await _providerPayActions.GetProviderNonPayableEventReceivedEvent(evaluation.EvaluationId)).Should().BeNull();
    }
}