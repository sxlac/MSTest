using Signify.EvaluationsApi.Core.Values;
using Signify.PAD.Svc.System.Tests.Core.Exceptions;

namespace Signify.PAD.Svc.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class CancelledEvaluationTests : CancelledEvaluationActions
{
    
    [RetryableTestMethod]
    public async Task ANC_T795_Cancelled_Without_Finalizing()
    {
        // Arrange
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();
        var answersDict = GeneratePerformedAnswers();
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,CoreApiActions.GetEvaluationAnswerList(answersDict));
        
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Cancel);
        
        CoreKafkaActions.PublishCdiEvent(evaluation.EvaluationId);
        
        // Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        this.Invoking(t=>t.GetProviderPayResultsWithEvalId(evaluation.EvaluationId, 15, 2))
            .Should().ThrowAsync<PadNotFoundException>();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        
        //  Validate that a Kafka event - ProviderPayRequestSent - was not raised
        var pprseTask = GetProviderPayRequestSentEvent(evaluation.EvaluationId);
        
        //  Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        var pperTask = GetProviderPayableEventReceivedEvent(evaluation.EvaluationId);
        
        //  Validate that the Kafka event - ProviderNonPayableEventReceived - was not raised
        var pnperTask = GetProviderNonPayableEventReceivedEvent(evaluation.EvaluationId);
        
        var tasks = new List<Task> { pprseTask, pperTask, pnperTask };
        
        while (tasks.Count > 0)
        {
            var finished = await Task.WhenAny(tasks);
            if (finished == pprseTask)
            {
                (await pprseTask).Should().BeNull();
            }
            else if (finished == pperTask)
            {
                (await pperTask).Should().BeNull();
            }
            else if (finished == pnperTask)
            {
                (await  pnperTask).Should().BeNull();
            }
            tasks.Remove(finished);
        }
        
    }

    [RetryableTestMethod]
    public async Task ANC_T796_Cancelled_Before_Finalizing()
    {
        // Arrange
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();
        var answersDict = GeneratePerformedAnswers();
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,CoreApiActions.GetEvaluationAnswerList(answersDict));

        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Cancel);
        
        CoreKafkaActions.PublishCdiEvent(evaluation.EvaluationId, false);
        
        await this.Invoking(t=>t.GetPadByEvaluationId(evaluation.EvaluationId, 15, 2))
            .Should().ThrowAsync<PadNotFoundException>();
        
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        CoreKafkaActions.PublishCdiEvent(evaluation.EvaluationId);
        
        //  Validate that the Kafka event - ProviderPayableEventReceived - was raised
        var pperTask = GetProviderPayableEventReceivedEvent(evaluation.EvaluationId);
        
        //  Validate that a Kafka event - ProviderPayRequestSent - was raised
        var pprseTask = GetProviderPayRequestSentEvent(evaluation.EvaluationId);
        
        var tasks = new List<Task> { pprseTask, pperTask };
        
        while (tasks.Count > 0)
        {
            var finished = await Task.WhenAny(tasks);
            if (finished == pprseTask)
            {
                (await pprseTask).Should().NotBeNull();
            }
            else if (finished == pperTask)
            {
                (await pperTask).Should().NotBeNull();
            }
            tasks.Remove(finished);
        }

        // Validate that there is entry in the ProviderPay table
        (await GetProviderPayResultsWithEvalId(evaluation.EvaluationId, 15, 2)).Should().NotBe(null);
        
    }

    [RetryableTestMethod]
    public async Task ANC_T797_Finalized_Without_Cancelling()
    {
        // Arrange
        var member = await CoreApiActions.CreateMember();
        var appointment = CoreApiActions.CreateAppointment(member.MemberPlanId, ["HHRA", "HBA1CPOC"]);
        var evaluation = CoreApiActions.CreateEvaluation(appointment.AppointmentId,member.MemberPlanId, ["HHRA", "HBA1CPOC"]);
        var answersDict = new Dictionary<int, string>
        {
            { 33070, "1" },
            { 33088, DateTime.Now.Date.ToString("O") },
            { 33264, DateTime.Now.AddDays(30).Date.ToString("O") },
            { 22034, DateTime.Now.Date.ToString("O") }
        };
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);

        CoreKafkaActions.PublishCdiEvent(evaluation.EvaluationId);
        
        // Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        this.Invoking(t=>t.GetProviderPayResultsWithEvalId(evaluation.EvaluationId, 15, 2))
            .Should().ThrowAsync<PadNotFoundException>();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        
        //  Validate that a Kafka event - ProviderPayRequestSent - was not raised
        var pprseTask = GetProviderPayRequestSentEvent(evaluation.EvaluationId);
        
        //  Validate that the Kafka event - ProviderPayableEventReceived - was not raised
        var pperTask = GetProviderPayableEventReceivedEvent(evaluation.EvaluationId);
        
        //  Validate that the Kafka event - ProviderNonPayableEventReceived - was not raised
        var pnperTask = GetProviderNonPayableEventReceivedEvent(evaluation.EvaluationId);
        
        var tasks = new List<Task> { pprseTask, pperTask, pnperTask };
        
        while (tasks.Count > 0)
        {
            var finished = await Task.WhenAny(tasks);
            if (finished == pprseTask)
            {
                (await pprseTask).Should().BeNull();
            }
            else if (finished == pperTask)
            {
                (await pperTask).Should().BeNull();
            }
            else if (finished == pnperTask)
            {
                (await  pnperTask).Should().BeNull();
            }
            tasks.Remove(finished);
        }
    }
}