using Signify.Dps.Test.Utilities.Database.Exceptions;
using Signify.EvaluationsApi.Core.Values;
using Signify.QE.MSTest.Attributes;
using Signify.uACR.System.Tests.Core.Models.Kafka;

namespace Signify.uACR.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class CancelledEvalTests : CancelledEvalActions
{
    public TestContext TestContext { get; set; }

    [RetryableTestMethod]
    public async Task ANC_T806_Cancelled_Without_Finalizing()
    {
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();
        
        TestContext.WriteLine($"[{TestContext.TestName}]EvaluationID: " + evaluation.EvaluationId);
        
        var barCode = GetBarcode();
        var alphaCode = GetAlphaCode();
        var answersDict = GeneratePerformedAnswers(barCode, alphaCode);
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Cancel);
        
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
        
        this.Invoking(async t=> await GetExamByEvaluationId(evaluation.EvaluationId))
            .Should().ThrowAsync<ExamNotFoundException>().GetAwaiter().GetResult()
            .WithMessage($"EvaluationId {evaluation.EvaluationId} not found in Exam table");
        
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
    public async Task ANC_T807_Cancelled_Before_Finalizing()
    {
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();
        
        TestContext.WriteLine($"[{TestContext.TestName}]EvaluationID: " + evaluation.EvaluationId);
        
        var barCode = GetBarcode();
        var alphaCode = GetAlphaCode();
        var answersDict = GeneratePerformedAnswers(barCode, alphaCode);
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Cancel);
        
        // Publish CdiFailed Kafka Event
        // CoreKafkaActions.PublishCdiEvent(evaluation.EvaluationId, false);
        
        this.Invoking(async t=>await t.GetExamByEvaluationId(evaluation.EvaluationId))
            .Should().ThrowAsync<ExamNotFoundException>().GetAwaiter().GetResult()
            .WithMessage($"EvaluationId {evaluation.EvaluationId} not found in Exam table");
        
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        var exam = await GetExamByEvaluationId(evaluation.EvaluationId);
        exam.ExamId.Should().NotBe(null);
        
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
        
        // Publish CdiPassed Kafka Event
        CoreKafkaActions.PublishCdiEvent(evaluation.EvaluationId, Product);
        
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
        (await GetProviderPayByExamId(exam.ExamId)).Should().NotBe(null);
    }
    
    [RetryableTestMethod]
    public async Task ANC_T808_Finalized_Without_Cancelling()
    {
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
        TestContext.WriteLine($"[{TestContext.TestName}]EvaluationID: " + evaluation.EvaluationId);
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
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
        
        CoreKafkaActions.PublishCdiEvent(evaluation.EvaluationId, Product);

        this.Invoking(async t=> await GetExamByEvaluationId(evaluation.EvaluationId))
            .Should().ThrowAsync<ExamNotFoundException>().GetAwaiter().GetResult()
            .WithMessage($"EvaluationId {evaluation.EvaluationId} not found in Exam table");
        
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