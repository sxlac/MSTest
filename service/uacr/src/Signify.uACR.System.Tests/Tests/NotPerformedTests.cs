using Signify.EvaluationsApi.Core.Values;
using Signify.QE.MSTest.Attributes;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Events.Status;
using Signify.uACR.System.Tests.Core.Constants;
using NotPerformedReason = Signify.uACR.System.Tests.Core.Models.Database.NotPerformedReason;

namespace Signify.uACR.System.Tests.Tests;

[TestClass, TestCategory("regression"), TestCategory("prod_smoke")]
public class NotPerformedTests :  NotPerformedActions
{
    public TestContext TestContext { get; set; }

    [RetryableTestMethod]
    [DynamicData(nameof(GetNotPerformedReasonsData))]
    public async Task ANC_T752_NotPerformed(int answerId, string reason)
    {
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();

        TestContext.WriteLine($"[{TestContext.TestName}]EvaluationID: " + evaluation.EvaluationId);

        var answersDict = GenerateNotPerformedAnswers(answerId, reason);
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));

        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);

        // Assert
       var eval = CoreApiActions.GetEvaluation(evaluation.EvaluationId);
        eval.Status.Should().Be("Completed");

        // Validate Exam Database record
        var exam = await GetExamByEvaluationId(evaluation.EvaluationId);
        exam.ExamId.Should().NotBe(null);

        // Validate ExamStatuses in DB
        var expectedStatusCodes = new List<int>
        {
            ExamStatusCode.ExamNotPerformed.ExamStatusCodeId
        };
        
        await ValidateExamStatusCodesByEvaluationId(evaluation.EvaluationId, expectedStatusCodes);
        
        await ValidateNotPerformedReason(exam.ExamId, answerId, reason);
        
        // Validate Exam Status Kafka Event
        if (Environment.GetEnvironmentVariable("TEST_ENV")!.Equals("prod"))
            return;
        var examStatusEvent = await CoreKafkaActions.GetUacrNotPerformedStatusEvent<NotPerformed>(evaluation.EvaluationId);
        examStatusEvent.ReasonType.Should().Be(GetParentNotPerformedReason(answerId));
        examStatusEvent.Reason.Should().Be(reason);
        examStatusEvent.ReasonNotes.Should().Be(Answers.ReasonNotesAnswer);
    }
    
    [RetryableTestMethod]
    [DynamicData(nameof(GetKedNotPerformedReasonsData))]
    public async Task ANC_T1178_KedNotPerformed(Dictionary<int,string> answersDict, int reasonAnswerId)
    {
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();

        TestContext.WriteLine($"[{TestContext.TestName}]EvaluationID: " + evaluation.EvaluationId);
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));

        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);

        // Assert
        var eval = CoreApiActions.GetEvaluation(evaluation.EvaluationId);
        eval.Status.Should().Be("Completed");

        // Validate Exam Database record
        var exam = await GetExamByEvaluationId(evaluation.EvaluationId);
        exam.ExamId.Should().NotBe(null);

        // Validate ExamStatuses in DB
        var expectedStatusCodes = new List<int>
        {
            ExamStatusCode.ExamNotPerformed.ExamStatusCodeId
        };
        
        await ValidateExamStatusCodesByEvaluationId(evaluation.EvaluationId, expectedStatusCodes);
        
        await ValidateNotPerformedReason(exam.ExamId, ReasonAnswerIdMappings[reasonAnswerId], answersDict[reasonAnswerId]);
        
        await ValidateKafkaEvent(evaluation.EvaluationId, answersDict, reasonAnswerId);
        
    }
    
    public static IEnumerable<object[]> GetNotPerformedReasonsData
    {
        get{
            foreach (var fi in typeof(NotPerformedReasons).GetFields())
            {
                var x = (NotPerformedReason)fi.GetValue(null)!;
                yield return [x.AnswerId, x.Reason];
            }
        }
    }
    
    public static IEnumerable<object[]> GetKedNotPerformedReasonsData
    {
        get{
            return new[]
            {
                new object[]
                {
                    new Dictionary<int, string>
                    {
                        { Answers.KedNotPerformedAnswerId, "No" },
                        { Answers.KedUnableToPerformAnswerId, "Unable to perform" },
                        { Answers.KedTechnicalIssueAnswerId, "Technical issue" },
                        { Answers.ReasonNotesAnswerId, Answers.ReasonNotesAnswer },
                        { Answers.DoSAnswerId, DateTime.Now.ToString("O") }
                    },
                    Answers.KedTechnicalIssueAnswerId
                },
                [
                    new Dictionary<int, string>
                    {
                        { Answers.KedNotPerformedAnswerId, "No" },
                        { Answers.KedUnableToPerformAnswerId, "Unable to perform" },
                        { Answers.KedEnvironmentalIssueAnswerId, "Environmental issue" },
                        { Answers.ReasonNotesAnswerId, Answers.ReasonNotesAnswer },
                        { Answers.DoSAnswerId, DateTime.Now.ToString("O") }
                    },
                    Answers.KedEnvironmentalIssueAnswerId
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.KedNotPerformedAnswerId, "No" },
                        { Answers.KedUnableToPerformAnswerId, "Unable to perform" },
                        { Answers.KedNoSuppliesOrEquipmentAnswerId, "No supplies or equipment" },
                        { Answers.ReasonNotesAnswerId, Answers.ReasonNotesAnswer },
                        { Answers.DoSAnswerId, DateTime.Now.ToString("O") }
                    },
                    Answers.KedNoSuppliesOrEquipmentAnswerId
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.KedNotPerformedAnswerId, "No" },
                        { Answers.KedUnableToPerformAnswerId, "Unable to perform" },
                        { Answers.KedInsufficientTrainingAnswerId, "Insufficient training" },
                        { Answers.ReasonNotesAnswerId, Answers.ReasonNotesAnswer },
                        { Answers.DoSAnswerId, DateTime.Now.ToString("O") }
                    },
                    Answers.KedInsufficientTrainingAnswerId
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.KedNotPerformedAnswerId, "No" },
                        { Answers.KedUnableToPerformAnswerId, "Unable to perform" },
                        { Answers.KedMemberPhysicallyUnableAnswerId, "Member physically unable" },
                        { Answers.ReasonNotesAnswerId, Answers.ReasonNotesAnswer },
                        { Answers.DoSAnswerId, DateTime.Now.ToString("O") }
                    },
                    Answers.KedMemberPhysicallyUnableAnswerId
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.KedNotPerformedAnswerId, "No" },
                        { Answers.KedMemberRefusedAnswerId, "Member refused" },
                        { Answers.KedMemberApprehensionAnswerId, "Member apprehension" },
                        { Answers.ReasonNotesAnswerId, Answers.ReasonNotesAnswer },
                        { Answers.DoSAnswerId, DateTime.Now.ToString("O") }
                    },
                    Answers.KedMemberApprehensionAnswerId
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.KedNotPerformedAnswerId, "No" },
                        { Answers.KedMemberRefusedAnswerId, "Member refused" },
                        { Answers.KedScheduledToCompleteAnswerId, "Scheduled to complete" },
                        { Answers.ReasonNotesAnswerId, Answers.ReasonNotesAnswer },
                        { Answers.DoSAnswerId, DateTime.Now.ToString("O") }
                    },
                    Answers.KedScheduledToCompleteAnswerId
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.KedNotPerformedAnswerId, "No" },
                        { Answers.KedMemberRefusedAnswerId, "Member refused" },
                        { Answers.KedNotInterestedAnswerId, "Not interested" },
                        { Answers.ReasonNotesAnswerId, Answers.ReasonNotesAnswer },
                        { Answers.DoSAnswerId, DateTime.Now.ToString("O") }
                    },
                    Answers.KedNotInterestedAnswerId
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.KedNotPerformedAnswerId, "No" },
                        { Answers.KedMemberRefusedAnswerId, "Member refused" },
                        { Answers.KedMemberRecentlyCompletedAnswerId, "Member recently completed" },
                        { Answers.ReasonNotesAnswerId, Answers.ReasonNotesAnswer },
                        { Answers.DoSAnswerId, DateTime.Now.ToString("O") }
                    },
                    Answers.KedMemberRecentlyCompletedAnswerId
                ]
            };
        }
    }
}