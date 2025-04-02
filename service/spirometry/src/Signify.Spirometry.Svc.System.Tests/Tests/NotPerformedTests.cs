using Signify.Spirometry.Svc.System.Tests.Core.Actions;
using Signify.QE.MSTest.Attributes;
using Signify.EvaluationsApi.Core.Values;
using Signify.Spirometry.Svc.System.Tests.Core.Constants;

namespace Signify.Spirometry.Svc.System.Tests.Tests;

[TestClass,TestCategory("regression"), TestCategory("prod_smoke")]
public class NotPerformedTests: NotPerformedActions
{

    [RetryableTestMethod]
    [DynamicData(nameof(VariousNotPerformedAnswers))]
    public async Task ANC_T399_NotPerformedTests(Dictionary<int, string> answersDict)
    {
        // Arrange
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();

        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,
            CoreApiActions.GetEvaluationAnswerList(answersDict));

        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);

        // Assert
        var eval = CoreApiActions.GetEvaluation(evaluation.EvaluationId);
        Assert.AreEqual("Completed", eval.Status);

        var testingNotes = answersDict.TryGetValue(Answers.NotesAnswerId, out string value) ? value : null;
        
        if (answersDict.ContainsKey(Answers.UnablePerformAnswerId))
        {
            switch (answersDict.Keys.Last())
            {
                case Answers.TechnicalIssueAnswerId:
                    await Validate_NotPerformed_Kafka_Database(evaluation.EvaluationId, Answers.ProviderUnableAnswer,
                        NotPerformedReasons.TechnicalIssue.AnswerId, NotPerformedReasons.TechnicalIssue.Reason,
                        testingNotes);
                    break;
                case Answers.EnvironmentalIssueAnswerId:
                    await Validate_NotPerformed_Kafka_Database(evaluation.EvaluationId, Answers.ProviderUnableAnswer,
                        NotPerformedReasons.EnvironmentalIssue.AnswerId, NotPerformedReasons.EnvironmentalIssue.Reason,
                        testingNotes);
                    break;
                case Answers.NoSuppliesOrEquipmentAnswerId:
                    await Validate_NotPerformed_Kafka_Database(evaluation.EvaluationId, Answers.ProviderUnableAnswer,
                        NotPerformedReasons.NoSuppliesOrEquipment.AnswerId,
                        NotPerformedReasons.NoSuppliesOrEquipment.Reason, testingNotes);
                    break;
                case Answers.InsufficientTrainingAnswerId:
                    await Validate_NotPerformed_Kafka_Database(evaluation.EvaluationId, Answers.ProviderUnableAnswer,
                        NotPerformedReasons.InsufficientTraining.AnswerId,
                        NotPerformedReasons.InsufficientTraining.Reason, testingNotes);
                    break;
                case Answers.MemberPhysicallyUnableAnswerId:
                    await Validate_NotPerformed_Kafka_Database(evaluation.EvaluationId, Answers.ProviderUnableAnswer,
                        NotPerformedReasons.MemberPhysicallyUnable.AnswerId,
                        NotPerformedReasons.MemberPhysicallyUnable.Reason, testingNotes);
                    break;
                case Answers.MemberOutsideDemographicRangesAnswerId:
                    await Validate_NotPerformed_Kafka_Database(evaluation.EvaluationId, Answers.ProviderUnableAnswer,
                        NotPerformedReasons.MemberOutsideDemographicRanges.AnswerId,
                        NotPerformedReasons.MemberOutsideDemographicRanges.Reason, testingNotes);
                    break;
            }

            return;
        }

        if (answersDict.ContainsKey(Answers.MemberRefusedAnswerId))
        {
            switch (answersDict.Keys.Last())
            {
                case Answers.MemberScheduledToCompleteAnswerId:
                    await Validate_NotPerformed_Kafka_Database(evaluation.EvaluationId, Answers.MemberRefusedAnswer,
                    NotPerformedReasons.ScheduledToComplete.AnswerId,
                    NotPerformedReasons.ScheduledToComplete.Reason, testingNotes);
                    break;
                case Answers.MemberRecentlyCompletedAnswerId:
                    await Validate_NotPerformed_Kafka_Database(evaluation.EvaluationId, Answers.MemberRefusedAnswer,
                    NotPerformedReasons.MemberRecentlyCompleted.AnswerId,
                    NotPerformedReasons.MemberRecentlyCompleted.Reason, testingNotes);
                    break;
                case Answers.MemberApprehensionAnswerId:
                    await Validate_NotPerformed_Kafka_Database(evaluation.EvaluationId, Answers.MemberRefusedAnswer,
                    NotPerformedReasons.MemberApprehension.AnswerId, NotPerformedReasons.MemberApprehension.Reason,
                    testingNotes);
                    break;
                case Answers.MemberNotInterestedAnswerId:
                    await Validate_NotPerformed_Kafka_Database(evaluation.EvaluationId, Answers.MemberRefusedAnswer,
                    NotPerformedReasons.NotInterested.AnswerId, NotPerformedReasons.NotInterested.Reason,
                    testingNotes);
                    break;
            }
        }
    }
    
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
                        { Answers.DosAnswerId, DateTime.Now.ToString("O") },
                        { Answers.PerformedNoAnswerId, "No" },
                        { Answers.UnablePerformAnswerId, Answers.ProviderUnableAnswer },
                        { Answers.NotesAnswerId, "testing" },
                        { Answers.TechnicalIssueAnswerId, "Technical issue" }
                    }
                },
                [
                    new Dictionary<int, string>
                    {
                        { Answers.DosAnswerId, DateTime.Now.ToString("O") },
                        { Answers.PerformedNoAnswerId, "No" },
                        { Answers.UnablePerformAnswerId, Answers.ProviderUnableAnswer },
                        { Answers.NotesAnswerId, "testing" },
                        { Answers.EnvironmentalIssueAnswerId, "Environmental issue" }
                    }
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.DosAnswerId, DateTime.Now.ToString("O") },
                        { Answers.PerformedNoAnswerId, "No" },
                        { Answers.UnablePerformAnswerId, Answers.ProviderUnableAnswer },
                        { Answers.NotesAnswerId, "testing" },
                        { Answers.NoSuppliesOrEquipmentAnswerId, "No supplies or equipment" }
                    }
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.DosAnswerId, DateTime.Now.ToString("O") },
                        { Answers.PerformedNoAnswerId, "No" },
                        { Answers.UnablePerformAnswerId, Answers.ProviderUnableAnswer },
                        { Answers.NotesAnswerId, "testing" },
                        { Answers.InsufficientTrainingAnswerId, "Insufficient training" }
                    }
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.DosAnswerId, DateTime.Now.ToString("O") },
                        { Answers.PerformedNoAnswerId, "No" },
                        { Answers.UnablePerformAnswerId, Answers.ProviderUnableAnswer },
                        { Answers.NotesAnswerId, "testing" },
                        { Answers.MemberPhysicallyUnableAnswerId, "Member physically unable" }
                    }
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.DosAnswerId, DateTime.Now.ToString("O") },
                        { Answers.PerformedNoAnswerId, "No" },
                        { Answers.UnablePerformAnswerId, Answers.ProviderUnableAnswer },
                        { Answers.NotesAnswerId, "testing" },
                        { Answers.MemberOutsideDemographicRangesAnswerId, "Member outside demographic ranges" }
                    }
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.DosAnswerId, DateTime.Now.ToString("O") },
                        { Answers.PerformedNoAnswerId, "No" },
                        { Answers.MemberRefusedAnswerId, Answers.MemberRefusedAnswer },
                        { Answers.MemberScheduledToCompleteAnswerId, "Scheduled to complete" }
                    }
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.DosAnswerId, DateTime.Now.ToString("O") },
                        { Answers.PerformedNoAnswerId, "No" },
                        { Answers.MemberRefusedAnswerId, Answers.MemberRefusedAnswer },
                        { Answers.NotesAnswerId, "testing" },
                        { Answers.MemberRecentlyCompletedAnswerId, "Member recently completed" }
                    }
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.DosAnswerId, DateTime.Now.ToString("O") },
                        { Answers.PerformedNoAnswerId, "No" },
                        { Answers.MemberRefusedAnswerId, Answers.MemberRefusedAnswer },
                        { Answers.NotesAnswerId, "testing" },
                        { Answers.MemberApprehensionAnswerId, "Member apprehension" }
                    }
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.DosAnswerId, DateTime.Now.ToString("O") },
                        { Answers.PerformedNoAnswerId, "No" },
                        { Answers.MemberRefusedAnswerId, Answers.MemberRefusedAnswer },
                        { Answers.MemberNotInterestedAnswerId, "Not interested" }
                    }
                ],
            };
        }
    }
}