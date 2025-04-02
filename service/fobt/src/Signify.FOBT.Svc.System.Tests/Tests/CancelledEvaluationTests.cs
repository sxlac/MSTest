using Signify.FOBT.Svc.System.Tests.Core.Actions;
using Signify.FOBT.Svc.System.Tests.Core.Models.Kafka;
using Signify.FOBT.Svc.System.Tests.Core.Constants;
using Signify.QE.MSTest.Attributes;
using Signify.QE.MSTest.Utilities;
using Signify.Dps.Test.Utilities.DataGen;
using Signify.EvaluationsApi.Core.Values;
using Signify.FOBT.Svc.System.Tests.Core.Exceptions;

namespace Signify.FOBT.Svc.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class CancelledEvaluationTests: CancelledEvaluationActions
{
    [RetryableTestMethod]
    public async Task ANC_T833_Finalized_Without_Cancelling()
    {
        // Arrange
        var member = await CoreApiActions.CreateMember();
        //Setting product code other than Fobt so that the evaluation is not captured and added to Fobt database. This will lead to a missing evaluation scenario.
        var appointment = CoreApiActions.CreateAppointment(member.MemberPlanId, ["HHRA", "HBA1CPOC"]);
        var evaluation = CoreApiActions.CreateEvaluation(appointment.AppointmentId,member.MemberPlanId,["HHRA","HBA1CPOC"]);
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
        
        // CdiEvent published with FOBT product code
        CoreKafkaActions.PublishCdiEvent(evaluation.EvaluationId);
        
        // Assert
        this.Invoking(t=>t.GetProviderPayResultsWithEvalId(evaluation.EvaluationId, 15, 2))
            .Should().ThrowAsync<FOBTNotFoundException>();
        
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
    public async Task ANC_T832_Cancelled_Without_Finalizing()
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
        this.Invoking(t=>t.GetProviderPayResultsWithEvalId(evaluation.EvaluationId, 15, 2))
            .Should().ThrowAsync<FOBTNotFoundException>();
        
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
    public async Task ANC_T831_Cancelled_Before_Finalizing()
    {
        // Arrange
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();
        var answersDict = GeneratePerformedAnswers();
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,CoreApiActions.GetEvaluationAnswerList(answersDict));

        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Cancel);
        
        CoreKafkaActions.PublishCdiEvent(evaluation.EvaluationId, false);
        
        //Assert there's no records of this evaluation created in FOBT table
        await this.Invoking(t=>t.GetFOBTByEvaluationId(evaluation.EvaluationId, 15, 2))
            .Should().ThrowAsync<FOBTNotFoundException>();
        
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        CoreKafkaActions.PublishCdiEvent(evaluation.EvaluationId);
        
        // Assert record created in FOBT table
        var fobt = await GetFOBTByEvaluationId(evaluation.EvaluationId, 20, 5);
        var  orderCorrelationId= fobt.OrderCorrelationId;
        var examId = fobt.FOBTId;
        
        // Publish the homeaccess lab results
        var resultsReceivedValue = new HomeAccessLabResults()
        {
            EventId = DataGen.NewUuid(),
            CreatedDateTime= DateTime.Now,
            OrderCorrelationId = orderCorrelationId,
            Barcode = answersDict[Answers.Barcode],
            LabTestType= "FOBT",
            LabResults = "Negative",
            AbnormalIndicator = "N",
            Exception = "",
            CollectionDate = DateTime.Now,
            ServiceDate = DateTime.Now,
            ReleaseDate = DateTime.Now
        };
        CoreHomeAccessKafkaActions.PublishEvent<HomeAccessLabResults>("homeaccess_labresults",resultsReceivedValue,evaluation.EvaluationId.ToString(),"HomeAccessResultsReceived");
        await Task.Delay(5000);
        
        
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
    
    
    
}
    






