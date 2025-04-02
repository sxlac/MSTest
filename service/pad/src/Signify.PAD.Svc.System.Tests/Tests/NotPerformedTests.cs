using Signify.EvaluationsApi.Core.Values;
using Signify.PAD.Svc.System.Tests.Core.Constants;

namespace Signify.PAD.Svc.System.Tests.Tests;

[TestClass, TestCategory("regression"), TestCategory("prod_smoke")]
public class NotPerformedTests : NotPerformedActions
{
    
    [RetryableTestMethod]
    [DynamicData(nameof(VariousNotPerformedAnswers))]
    public async Task ANC_T354_NotPerformed(Dictionary<int, string> answersDict, int reasonAnswerId)
    {
        // Arrange
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);

        await Validate_NotPerformed_Kafka_Database(evaluation.EvaluationId, answersDict, reasonAnswerId);
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
                            { Answers.PerformedNoAnswerId, "No" },
                            { Answers.UnableToPerformAnswerId, "Unable to perform" },
                            { Answers.TechnicalIssueAnswerId, "Technical issue" },
                            { Answers.UnableToPerformNotesAnswerId, "Unable to perform notes." },
                            { Answers.DosAnswerId, DateTime.Now.ToString("O") }
                        },
                        Answers.TechnicalIssueAnswerId
                    },
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedNoAnswerId, "No" },
                            { Answers.UnableToPerformAnswerId, "Unable to perform" },
                            { Answers.EnvironmentalIssueAnswerId, "Environmental issue" },
                            { Answers.UnableToPerformNotesAnswerId, "Unable to perform notes." },
                            { Answers.DosAnswerId, DateTime.Now.ToString("O") }
                        },
                        Answers.EnvironmentalIssueAnswerId
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedNoAnswerId, "No" },
                            { Answers.UnableToPerformAnswerId, "Unable to perform" },
                            { Answers.NoSuppliesOrEquipmentAnswerId, "No supplies or equipment" },
                            { Answers.UnableToPerformNotesAnswerId, "Unable to perform notes." },
                            { Answers.DosAnswerId, DateTime.Now.ToString("O") }
                        },
                        Answers.NoSuppliesOrEquipmentAnswerId
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedNoAnswerId, "No" },
                            { Answers.UnableToPerformAnswerId, "Unable to perform" },
                            { Answers.InsufficientTrainingAnswerId, "Insufficient training" },
                            { Answers.UnableToPerformNotesAnswerId, "Unable to perform notes." },
                            { Answers.DosAnswerId, DateTime.Now.ToString("O") }
                        },
                        Answers.InsufficientTrainingAnswerId
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedNoAnswerId, "No" },
                            { Answers.UnableToPerformAnswerId, "Unable to perform" },
                            { Answers.MemberPhysicallyUnableAnswerId, "Member physically unable" },
                            { Answers.UnableToPerformNotesAnswerId, "Unable to perform notes." },
                            { Answers.DosAnswerId, DateTime.Now.ToString("O") }
                        },
                        Answers.MemberPhysicallyUnableAnswerId
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedNoAnswerId, "No" },
                            { Answers.MemberRefusedAnswerId, "Member refused" },
                            { Answers.MemberRecentlyCompletedAnswerId, "Member recently completed" },
                            { Answers.MemberRefusalNotesAnswerId, "Member refused notes." },
                            { Answers.DosAnswerId, DateTime.Now.ToString("O") }
                        },
                        Answers.MemberRecentlyCompletedAnswerId
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedNoAnswerId, "No" },
                            { Answers.MemberRefusedAnswerId, "Member refused" },
                            { Answers.ScheduledToCompleteAnswerId, "Scheduled to complete" },
                            { Answers.MemberRefusalNotesAnswerId, "Member refused notes." },
                            { Answers.DosAnswerId, DateTime.Now.ToString("O") }
                        },
                        Answers.ScheduledToCompleteAnswerId
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedNoAnswerId, "No" },
                            { Answers.MemberRefusedAnswerId, "Member refused" },
                            { Answers.MemberApprehensionAnswerId, "Member apprehension" },
                            { Answers.MemberRefusalNotesAnswerId, "Member refused notes." },
                            { Answers.DosAnswerId, DateTime.Now.ToString("O") }
                        },
                        Answers.MemberApprehensionAnswerId
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedNoAnswerId, "No" },
                            { Answers.MemberRefusedAnswerId, "Member refused" },
                            { Answers.NotInterestedAnswerId, "Not interested" },
                            { Answers.MemberRefusalNotesAnswerId, "Member refused notes." },
                            { Answers.DosAnswerId, DateTime.Now.ToString("O") }
                        },
                        Answers.NotInterestedAnswerId
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedNoAnswerId, "No" },
                            { Answers.MemberRefusedAnswerId, "Member refused" },
                            { Answers.OtherAnswerId, "Other" },
                            { Answers.MemberRefusalNotesAnswerId, "Member refused notes." },
                            { Answers.DosAnswerId, DateTime.Now.ToString("O") }
                        },
                        Answers.OtherAnswerId
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedNoAnswerId, "No" },
                            { Answers.ClinicallyIrrelevantAnswerId, "Not clinically relevant" },
                            { Answers.ClinicallyIrrelevantReasonAnswerId, "Clinically irrelevant reason." },
                            { Answers.DosAnswerId, DateTime.Now.ToString("O") }
                        },
                        Answers.ClinicallyIrrelevantAnswerId
                    ]
                };
            }
        }

    #endregion
}