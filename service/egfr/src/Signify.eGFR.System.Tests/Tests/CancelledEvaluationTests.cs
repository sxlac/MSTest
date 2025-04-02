using Signify.EvaluationsApi.Core.Values;
using Signify.eGFR.System.Tests.Core.Actions;
using Signify.eGFR.System.Tests.Core.Exceptions;
using Signify.QE.MSTest.Attributes;

namespace Signify.eGFR.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class CancelledEvaluationTests : CancelledEvaluationActions

{
    public TestContext TestContext { get; set; }

    [RetryableTestMethod]
    public async Task ANC_T817_Cancelled_Without_Finalizing()
    {
        // Arrange
        var (member, appointment, evaluation) =
            await CoreApiActions.PrepareEvaluation();
        var answersDict = GeneratePerformedAnswers();
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,CoreApiActions.GetEvaluationAnswerList(answersDict));
        
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Cancel);
        
        CoreKafkaActions.PublishCdiEvent(evaluation.EvaluationId, Product);
        
        // Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        this.Invoking(t=>t.GetProviderPayResultsWithEvalId(evaluation.EvaluationId))
            .Should().ThrowAsync<ExamNotFoundException>();
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
    public async Task ANC_T818_Cancelled_Before_Finalizing()
    {
        // Arrange
        var (member, appointment, evaluation) =
            await CoreApiActions.PrepareEvaluation();
        var answersDict = GeneratePerformedAnswers();
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,CoreApiActions.GetEvaluationAnswerList(answersDict));

        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Cancel);
        
        CoreKafkaActions.PublishCdiEvent(evaluation.EvaluationId, Product, false);
        
        await this.Invoking(t=>t.GetExamByEvaluationId(evaluation.EvaluationId))
            .Should().ThrowAsync<ExamNotFoundException>();
        
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        CoreKafkaActions.PublishCdiEvent(evaluation.EvaluationId, Product);
        
    }

    [RetryableTestMethod]
    public async Task ANC_T819_Finalized_Without_Cancelling()
    {
        // Arrange
        var (member, appointment, evaluation) =
            await CoreApiActions.PrepareEvaluation();
        var answersDict = GenerateKedPerformedAnswers();
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,CoreApiActions.GetEvaluationAnswerList(answersDict));

        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);

        CoreKafkaActions.PublishCdiEvent(evaluation.EvaluationId, Product);
        
        // Assert
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        this.Invoking(t=>t.GetProviderPayResultsWithEvalId(evaluation.EvaluationId))
            .Should().ThrowAsync<ExamNotFoundException>();
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