using Signify.DEE.Messages.Status;
using Signify.DEE.Svc.System.Tests.Core.Constants;
using Signify.EvaluationsApi.Core.Values;

namespace Signify.DEE.Svc.System.Tests.Tests;

[TestClass,TestCategory("regression"), TestCategory("prod_smoke")]
public class NotPerformedTests : NotPerformedActions
{
    public TestContext TestContext { get; set; }

    [RetryableTestMethod]
    [DynamicData(nameof(VariousNotPerformedAnswers))]
    public async Task ANC_T1168_NotPerformed_Test(Dictionary<int, string> answersDict, int reasonId)
    {
        // Act
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();
        TestContext.WriteLine($"[{TestContext.TestName}] - EvaluationId: {evaluation.EvaluationId}");

        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);

        // Assert
        var exam = await GetExamByEvaluationId(evaluation.EvaluationId);
        
        var expectedStatusCodes = new List<int>
        {
            ExamStatusCodes.ExamCreated.ExamStatusCodeId,
            ExamStatusCodes.DeeNotPerformed.ExamStatusCodeId,
            ExamStatusCodes.NoDeeImagesTaken.ExamStatusCodeId  
        };
        await ValidateExamStatusCodesByExamId(exam.ExamId, expectedStatusCodes);
        
        var notPerformed = await GetNotPerformedReasonByExamId(exam.ExamId);
        
        switch (reasonId)
        {
            case Answers.EnvironmentalIssueAnswerId:
                Assert.AreEqual(NotPerformedReasons.EnvironmentalIssue.NotPerformedReasonId, notPerformed.NotPerformedReasonId);
                Assert.AreEqual("", notPerformed.Notes);
                break;
            case Answers.TechnicalIssueAnswerId:
                Assert.AreEqual(NotPerformedReasons.TechnicalIssue.NotPerformedReasonId, notPerformed.NotPerformedReasonId);
                Assert.AreEqual("", notPerformed.Notes);
                break;
            case Answers.NoSuppliesOrEquipmentAnswerId:
                Assert.AreEqual(NotPerformedReasons.NoSuppliesOrEquipment.NotPerformedReasonId, notPerformed.NotPerformedReasonId);
                Assert.AreEqual("", notPerformed.Notes);
                break;
            case Answers.InsufficientTrainingAnswerId:
                Assert.AreEqual(NotPerformedReasons.InsufficientTraining.NotPerformedReasonId, notPerformed.NotPerformedReasonId);
                Assert.AreEqual("", notPerformed.Notes);
                break;
            case Answers.MemberPhysicallyUnableAnswerId:
                Assert.AreEqual(NotPerformedReasons.MemberPhysicallyUnable.NotPerformedReasonId, notPerformed.NotPerformedReasonId);
                Assert.AreEqual("", notPerformed.Notes);
                break;
            case Answers.MemberApprehensionAnswerId:
                Assert.AreEqual(NotPerformedReasons.MemberApprehension.NotPerformedReasonId, notPerformed.NotPerformedReasonId);
                Assert.AreEqual("", notPerformed.Notes);
                break;
            case Answers.NotInterestedAnswerId:
                Assert.AreEqual(NotPerformedReasons.NotInterested.NotPerformedReasonId, notPerformed.NotPerformedReasonId);
                Assert.AreEqual("", notPerformed.Notes);
                break;
            case Answers.MemberRecentlyCompletedAnswerId:
                Assert.AreEqual(NotPerformedReasons.MemberRecentlyCompleted.NotPerformedReasonId, notPerformed.NotPerformedReasonId);
                Assert.AreEqual("", notPerformed.Notes);
                break;
            case Answers.ScheduledToCompleteAnswerId:
                Assert.AreEqual(NotPerformedReasons.ScheduledToComplete.NotPerformedReasonId, notPerformed.NotPerformedReasonId);
                Assert.AreEqual("", notPerformed.Notes);
                break;
            case Answers.MemberRefusedOtherReasonAnswerId:
                Assert.AreEqual(NotPerformedReasons.MemberRefusedOther.NotPerformedReasonId, notPerformed.NotPerformedReasonId);
                Assert.AreEqual(answersDict[Answers.MemberRefusedOtherReasonAnswerId], notPerformed.Notes);
                break;
            case Answers.UnableToPerformOtherReasonAnswerId:
                Assert.AreEqual(NotPerformedReasons.UnableToPerformOther.NotPerformedReasonId, notPerformed.NotPerformedReasonId);
                Assert.AreEqual(answersDict[Answers.UnableToPerformOtherReasonAnswerId], notPerformed.Notes);
                break;
        }
        
        // Remove once  kafka validation in prod is enabled
        if (Environment.GetEnvironmentVariable("TEST_ENV")!.Equals("prod")) return;
        
        var deeStatusEvent = await CoreKafkaActions.GetDeeNotPerformedStatusEvent<NotPerformed>(evaluation.EvaluationId);
        switch (reasonId)
        {
            case Answers.EnvironmentalIssueAnswerId:
                Assert.AreEqual(Answers.UnableToPerformAnswer, deeStatusEvent.ReasonType);
                Assert.AreEqual(NotPerformedReasons.EnvironmentalIssue.Reason, deeStatusEvent.Reason);
                Assert.AreEqual("", deeStatusEvent.ReasonNotes);
                break;
            case Answers.TechnicalIssueAnswerId:
                Assert.AreEqual(Answers.UnableToPerformAnswer, deeStatusEvent.ReasonType);
                Assert.AreEqual(NotPerformedReasons.TechnicalIssue.Reason, deeStatusEvent.Reason);
                Assert.AreEqual("", deeStatusEvent.ReasonNotes);
                break;
            case Answers.NoSuppliesOrEquipmentAnswerId:
                Assert.AreEqual(Answers.UnableToPerformAnswer, deeStatusEvent.ReasonType);
                Assert.AreEqual(NotPerformedReasons.NoSuppliesOrEquipment.Reason, deeStatusEvent.Reason);
                Assert.AreEqual("", deeStatusEvent.ReasonNotes);
                break;
            case Answers.InsufficientTrainingAnswerId:
                Assert.AreEqual(Answers.UnableToPerformAnswer, deeStatusEvent.ReasonType);
                Assert.AreEqual(NotPerformedReasons.InsufficientTraining.Reason, deeStatusEvent.Reason);
                Assert.AreEqual("", deeStatusEvent.ReasonNotes);
                break;
            case Answers.MemberPhysicallyUnableAnswerId:
                Assert.AreEqual(Answers.UnableToPerformAnswer, deeStatusEvent.ReasonType);
                Assert.AreEqual(NotPerformedReasons.MemberPhysicallyUnable.Reason, deeStatusEvent.Reason);
                Assert.AreEqual("", deeStatusEvent.ReasonNotes);
                break;
            case Answers.MemberApprehensionAnswerId:
                Assert.AreEqual(Answers.MemberRefusedAnswer, deeStatusEvent.ReasonType);
                Assert.AreEqual(NotPerformedReasons.MemberApprehension.Reason, deeStatusEvent.Reason);
                Assert.AreEqual("", deeStatusEvent.ReasonNotes);
                break;
            case Answers.NotInterestedAnswerId:
                Assert.AreEqual(Answers.MemberRefusedAnswer, deeStatusEvent.ReasonType);
                Assert.AreEqual(NotPerformedReasons.NotInterested.Reason, deeStatusEvent.Reason);
                Assert.AreEqual("", deeStatusEvent.ReasonNotes);
                break;
            case Answers.MemberRecentlyCompletedAnswerId:
                Assert.AreEqual(Answers.MemberRefusedAnswer, deeStatusEvent.ReasonType);
                Assert.AreEqual(NotPerformedReasons.MemberRecentlyCompleted.Reason, deeStatusEvent.Reason);
                Assert.AreEqual("", deeStatusEvent.ReasonNotes);
                break;
            case Answers.ScheduledToCompleteAnswerId:
                Assert.AreEqual(Answers.MemberRefusedAnswer, deeStatusEvent.ReasonType);
                Assert.AreEqual(NotPerformedReasons.ScheduledToComplete.Reason, deeStatusEvent.Reason);
                Assert.AreEqual("", deeStatusEvent.ReasonNotes);
                break;
            case Answers.MemberRefusedOtherReasonAnswerId:
                Assert.AreEqual(Answers.MemberRefusedAnswer, deeStatusEvent.ReasonType);
                Assert.AreEqual(NotPerformedReasons.MemberRefusedOther.Reason, deeStatusEvent.Reason);
                Assert.AreEqual(answersDict[Answers.MemberRefusedOtherReasonAnswerId], deeStatusEvent.ReasonNotes);
                break;
            case Answers.UnableToPerformOtherReasonAnswerId:
                Assert.AreEqual(Answers.UnableToPerformAnswer, deeStatusEvent.ReasonType);
                Assert.AreEqual(NotPerformedReasons.UnableToPerformOther.Reason, deeStatusEvent.Reason);
                Assert.AreEqual(answersDict[Answers.UnableToPerformOtherReasonAnswerId], deeStatusEvent.ReasonNotes);
                break;
        }
        
    }

    #region TestData

    private static IEnumerable<object[]> VariousNotPerformedAnswers
    {
        get
        {
            return new[]
            {
                new object[]
                {
                    new Dictionary<int, string>
                    {
                        { Answers.DeeNotPerformedAnswerId, "1" },
                        { Answers.MemberRefusedAnswerId, "Yes" },
                        { Answers.ScheduledToCompleteAnswerId, "Yes" },
                        { Answers.GenderAnswerId, "Yes" },
                        { Answers.StateAnswerId, "Yes" },
                        { Answers.FirstNameAnswerId, "Yes" },
                        { Answers.LastNameAnswerId, "Yes" },
                    },
                    Answers.ScheduledToCompleteAnswerId
                },
                [
                    new Dictionary<int, string>
                    {
                        { Answers.DeeNotPerformedAnswerId, "1" },
                        { Answers.MemberRefusedAnswerId, "Yes" },
                        { Answers.MemberRecentlyCompletedAnswerId, "Yes" },
                        { Answers.GenderAnswerId, "Yes" },
                        { Answers.StateAnswerId, "Yes" },
                        { Answers.FirstNameAnswerId, "Yes" },
                        { Answers.LastNameAnswerId, "Yes" },
                    },
                    Answers.MemberRecentlyCompletedAnswerId
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.DeeNotPerformedAnswerId, "1" },
                        { Answers.MemberRefusedAnswerId, "Yes" },
                        { Answers.MemberApprehensionAnswerId, "Yes" },
                        { Answers.GenderAnswerId, "Yes" },
                        { Answers.StateAnswerId, "Yes" },
                        { Answers.FirstNameAnswerId, "Yes" },
                        { Answers.LastNameAnswerId, "Yes" },
                    },
                    Answers.MemberApprehensionAnswerId
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.DeeNotPerformedAnswerId, "1" },
                        { Answers.MemberRefusedAnswerId, "Yes" },
                        { Answers.NotInterestedAnswerId, "Yes" },
                        { Answers.GenderAnswerId, "Yes" },
                        { Answers.StateAnswerId, "Yes" },
                        { Answers.FirstNameAnswerId, "Yes" },
                        { Answers.LastNameAnswerId, "Yes" },
                    }, 
                    Answers.NotInterestedAnswerId
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.DeeNotPerformedAnswerId, "1" },
                        { Answers.MemberRefusedAnswerId, "Yes" },
                        { Answers.MemberPhysicallyUnableAnswerId, "Yes" },
                        { Answers.GenderAnswerId, "Yes" },
                        { Answers.StateAnswerId, "Yes" },
                        { Answers.FirstNameAnswerId, "Yes" },
                        { Answers.LastNameAnswerId, "Yes" },
                    },
                    Answers.MemberPhysicallyUnableAnswerId
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.DeeNotPerformedAnswerId, "1" },
                        { Answers.MemberRefusedAnswerId, "Yes" },
                        { Answers.MemberRefusedOtherReasonAnswerId, "Yes" },
                        { Answers.MemberRefusedNotesAnswerId, "Yes" },
                        { Answers.GenderAnswerId, "Yes" },
                        { Answers.StateAnswerId, "Yes" },
                        { Answers.FirstNameAnswerId, "Yes" },
                        { Answers.LastNameAnswerId, "Yes" },
                    },
                    Answers.MemberRefusedOtherReasonAnswerId
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.DeeNotPerformedAnswerId, "1" },
                        { Answers.UnableToPerformAnswerId, "Yes" },
                        { Answers.EnvironmentalIssueAnswerId, "Yes" },
                        { Answers.GenderAnswerId, "Yes" },
                        { Answers.StateAnswerId, "Yes" },
                        { Answers.FirstNameAnswerId, "Yes" },
                        { Answers.LastNameAnswerId, "Yes" },
                    },
                    Answers.EnvironmentalIssueAnswerId
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.DeeNotPerformedAnswerId, "1" },
                        { Answers.UnableToPerformAnswerId, "Yes" },
                        { Answers.TechnicalIssueAnswerId, "Yes" },
                        { Answers.GenderAnswerId, "Yes" },
                        { Answers.StateAnswerId, "Yes" },
                        { Answers.FirstNameAnswerId, "Yes" },
                        { Answers.LastNameAnswerId, "Yes" },
                    },
                    Answers.TechnicalIssueAnswerId
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.DeeNotPerformedAnswerId, "1" },
                        { Answers.UnableToPerformAnswerId, "Yes" },
                        { Answers.InsufficientTrainingAnswerId, "Yes" },
                        { Answers.GenderAnswerId, "Yes" },
                        { Answers.StateAnswerId, "Yes" },
                        { Answers.FirstNameAnswerId, "Yes" },
                        { Answers.LastNameAnswerId, "Yes" },
                    },
                    Answers.InsufficientTrainingAnswerId
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.DeeNotPerformedAnswerId, "1" },
                        { Answers.UnableToPerformAnswerId, "Yes" },
                        { Answers.NoSuppliesOrEquipmentAnswerId, "Yes" },
                        { Answers.GenderAnswerId, "Yes" },
                        { Answers.StateAnswerId, "Yes" },
                        { Answers.FirstNameAnswerId, "Yes" },
                        { Answers.LastNameAnswerId, "Yes" },
                    },
                    Answers.NoSuppliesOrEquipmentAnswerId
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.DeeNotPerformedAnswerId, "1" },
                        { Answers.UnableToPerformAnswerId, "Yes" },
                        { Answers.UnableToPerformOtherReasonAnswerId, "Yes" },
                        { Answers.UnableToPerformNotesAnswerId, "Yes" },
                        { Answers.GenderAnswerId, "Yes" },
                        { Answers.StateAnswerId, "Yes" },
                        { Answers.FirstNameAnswerId, "Yes" },
                        { Answers.LastNameAnswerId, "Yes" },
                    },
                    Answers.UnableToPerformOtherReasonAnswerId
                ]
            };
        }
    }
    
    #endregion
}