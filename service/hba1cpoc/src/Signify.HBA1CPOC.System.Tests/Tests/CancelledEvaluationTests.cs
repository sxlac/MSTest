using Signify.EvaluationsApi.Core.Values;
using Signify.HBA1CPOC.System.Tests.Core.Constants;
using Signify.QE.MSTest.Attributes;

namespace Signify.HBA1CPOC.System.Tests.Tests;

[TestClass,TestCategory("regression")]
public class CancelledEvaluationTests : CancelledEvaluationActions 
{
    public TestContext TestContext { get; set; }
    private readonly ProviderPayActions _providerPayActions = new ();

    [RetryableTestMethod]
    public async Task ANC_T1030_Cancelled_Without_Finalizing()
    {
        // Arrange
        var (member,appointment,evaluation) = await CoreApiActions.PrepareEvaluation();
        var answers = GeneratePerformedAnswers();
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,CoreApiActions.GetEvaluationAnswerList(answers));
        
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Cancel);
        
        // Assert
        Assert.ThrowsException<Exception>(() => GetHba1CpocRecordByEvaluationId(evaluation.EvaluationId));

        CoreKafkaActions.PublishCdiEvent(evaluation.EvaluationId, Product);
        
        GetProviderPayResultsWithEvalId(evaluation.EvaluationId).Should().BeNull();
        
        //  Validate that a Kafka event - ProviderPayRequestSent - was not raised
        (await _providerPayActions.GetProviderPayRequestSentEvent(evaluation.EvaluationId)).Should().BeNull();
        //  Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        (await _providerPayActions.GetProviderPayableEventReceivedEvent(evaluation.EvaluationId)).Should().BeNull();
        (await _providerPayActions.GetProviderNonPayableEventReceivedEvent(evaluation.EvaluationId)).Should().BeNull();
    }

    [RetryableTestMethod]
    public async Task ANC_T1029_Cancelled_Before_Finalizing()
    {
        // Arrange
        var (member,appointment,evaluation) = await CoreApiActions.PrepareEvaluation();
        var answers = GeneratePerformedAnswers();
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,CoreApiActions.GetEvaluationAnswerList(answers));
        
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Cancel);
        
        CoreKafkaActions.PublishCdiEvent(evaluation.EvaluationId, Product, false);
        
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);

        var exam = GetHba1CpocRecordByEvaluationId(evaluation.EvaluationId);
        exam.HBA1CPOCId.Should().NotBe(null);
        
        CoreKafkaActions.PublishCdiEvent(evaluation.EvaluationId, Product);

        ValidateExamStatusCodesByEvaluationId(evaluation.EvaluationId,[HBA1CPOCStatusCodes.CdiPassedReceived.HBA1CPOCStatusCodeId]);
        
    }
    
    [RetryableTestMethod]
    public async Task ANC_T1031_Finalized_Without_Cancelling()
    {
        // Arrange
        var member = await CoreApiActions.CreateMember();
        var appointment = CoreApiActions.CreateAppointment(member.MemberPlanId, ["HHRA", "PAD"]);
        var evaluation = CoreApiActions.CreateEvaluation(appointment.AppointmentId, member.MemberPlanId, ["HHRA", "PAD"]);
        
        var answers = new Dictionary<int, string>
        {
            { 29560, "1" },
            { 29564, "1" },
            { 30973, "1" },
            { 22034, DateTime.Now.Date.ToString("O") }
        };
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,CoreApiActions.GetEvaluationAnswerList(answers));
        
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        // Assert
        Assert.ThrowsException<Exception>(() => GetHba1CpocRecordByEvaluationId(evaluation.EvaluationId));

        CoreKafkaActions.PublishCdiEvent(evaluation.EvaluationId, Product);
        
        GetProviderPayResultsWithEvalId(evaluation.EvaluationId).Should().BeNull();
        
        //  Validate that a Kafka event - ProviderPayRequestSent - was not raised
        (await _providerPayActions.GetProviderPayRequestSentEvent(evaluation.EvaluationId)).Should().BeNull();
        //  Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        (await _providerPayActions.GetProviderPayableEventReceivedEvent(evaluation.EvaluationId)).Should().BeNull();
        (await _providerPayActions.GetProviderNonPayableEventReceivedEvent(evaluation.EvaluationId)).Should().BeNull();
    }

}   