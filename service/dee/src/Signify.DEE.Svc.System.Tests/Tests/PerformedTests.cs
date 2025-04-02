using Signify.DEE.Messages.Status;
using Signify.DEE.Svc.Core.Events;
using Signify.DEE.Svc.System.Tests.Core.Constants;
using Signify.EvaluationsApi.Core.Values;

namespace Signify.DEE.Svc.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class PerformedTests : BaseTestActions
{
    public TestContext TestContext { get; set; }
    
    [RetryableTestMethod]
    public async Task ANC_T1167_Performed_Test()
    {
        // Act
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();
        TestContext.WriteLine($"[{TestContext.TestName}] - EvaluationId: {evaluation.EvaluationId}");
        
        var answers = GeneratePerformedAnswers();
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, GetEvaluationAnswerListWithImages(answers));
        
        // Wait for a few seconds to allow the evaluation answers to be processed
        await Task.Delay(6000);
        
        // Assert
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        var finalizedEvent = await CoreKafkaActions.GetEvaluationEvent<EvaluationFinalizedEvent>(evaluation.EvaluationId, "EvaluationFinalizedEvent");
        
        var exam = await GetExamByEvaluationId(evaluation.EvaluationId);
        Assert.AreEqual(exam.AppointmentId, appointment.AppointmentId);
        Assert.AreEqual(exam.ProviderId, TestConstants.Provider.ProviderId);
        Assert.AreEqual(exam.ClientId, appointment.ClientId);
        Assert.AreEqual(exam.State, member.State);
        Assert.AreEqual(exam.MemberPlanId, member.MemberPlanId);
        Assert.AreEqual(exam.DateOfService, finalizedEvent.DateOfService);

        var performedEvent = await CoreKafkaActions.GetDeePerformedStatusEvent<Performed>(evaluation.EvaluationId);
        Assert.AreEqual(performedEvent.ProviderId, TestConstants.Provider.ProviderId);
        Assert.AreEqual(performedEvent.MemberPlanId, member.MemberPlanId);
        Assert.AreEqual(performedEvent.CreateDate.ToString().Split("+")[0].Trim(), exam.CreatedDateTime.ToString().Split(".")[0]);
        Assert.AreEqual(performedEvent.ReceivedDate.ToString().Split("+")[0].Trim(), exam.ReceivedDateTime.ToString().Split(".")[0]);
        
        var expectedStatusCodes = new List<int>
        {
            ExamStatusCodes.ExamCreated.ExamStatusCodeId,
            // ExamStatusCodes.IrisImageReceived.ExamStatusCodeId, // This status code is not sent by IRIS at this time being in UAT
            ExamStatusCodes.DeeImagesFound.ExamStatusCodeId,
            // ExamStatusCodes.IrisExamCreated.ExamStatusCodeId, // This status code is not sent by IRIS at this time being in UAT
            ExamStatusCodes.DeePerformed.ExamStatusCodeId,
            ExamStatusCodes.IrisOrderSubmitted.ExamStatusCodeId,
            ExamStatusCodes.IrisImagesSubmitted.ExamStatusCodeId
            
        };
        await ValidateExamStatusCodesByExamId(exam.ExamId, expectedStatusCodes);

    }
    
}