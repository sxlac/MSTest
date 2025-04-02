using Signify.EvaluationsApi.Core.Values;
using Signify.eGFR.System.Tests.Core.Actions;
using Signify.eGFR.System.Tests.Core.Constants;
using Signify.eGFR.System.Tests.Core.Models.Database;
using Signify.QE.MSTest.Attributes;

namespace Signify.eGFR.System.Tests.Tests;

[TestClass, TestCategory("regression"), TestCategory("prod_smoke")]
public class NotPerformedTests : NotPerformedActions
{
    public TestContext TestContext { get; set; }
    
    [RetryableTestMethod]
    [DynamicData(nameof(GetNotPerformedReasonsData))]
    public async Task ANC_T411_NotPerformed(int answerId, string reason)
    {
        // Arrange
        var (member, appointment, evaluation) =
            await CoreApiActions.PrepareEvaluation();
        
        var answersDict = GenerateNotPerformedAnswers(answerId, reason);
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);

        await Validate_NotPerformed_Kafka_Database(evaluation.EvaluationId, answersDict, answerId);
        
    }
    
    [RetryableTestMethod]
    [DynamicData(nameof(GetKedNotPerformedReasonsData))]
    public async Task ANC_T1177_KedNotPerformed(Dictionary<int,string> answerDict, int reasonId)
    {
        // Arrange
        var (member, appointment, evaluation) =
            await CoreApiActions.PrepareEvaluation();
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answerDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);

        await Validate_NotPerformed_Kafka_Database(evaluation.EvaluationId, answerDict, reasonId);
        
    }

    #region TestData

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
                        { Answers.EgfrNotesAnswerId, "Unable to perform notes." },
                        { Answers.DosAnswerId, DateTime.Now.ToString("O") }
                    },
                    Answers.KedTechnicalIssueAnswerId
                },
                [
                    new Dictionary<int, string>
                    {
                        { Answers.KedNotPerformedAnswerId, "No" },
                        { Answers.KedUnableToPerformAnswerId, "Unable to perform" },
                        { Answers.KedEnvironmentalIssueAnswerId, "Environmental issue" },
                        { Answers.EgfrNotesAnswerId, "Unable to perform notes." },
                        { Answers.DosAnswerId, DateTime.Now.ToString("O") }
                    },
                    Answers.KedEnvironmentalIssueAnswerId
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.KedNotPerformedAnswerId, "No" },
                        { Answers.KedUnableToPerformAnswerId, "Unable to perform" },
                        { Answers.KedNoSuppliesOrEquipmentAnswerId, "No supplies or equipment" },
                        { Answers.EgfrNotesAnswerId, "Unable to perform notes." },
                        { Answers.DosAnswerId, DateTime.Now.ToString("O") }
                    },
                    Answers.KedNoSuppliesOrEquipmentAnswerId
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.KedNotPerformedAnswerId, "No" },
                        { Answers.KedUnableToPerformAnswerId, "Unable to perform" },
                        { Answers.KedInsufficientTrainingAnswerId, "Insufficient training" },
                        { Answers.EgfrNotesAnswerId, "Unable to perform notes." },
                        { Answers.DosAnswerId, DateTime.Now.ToString("O") }
                    },
                    Answers.KedInsufficientTrainingAnswerId
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.KedNotPerformedAnswerId, "No" },
                        { Answers.KedUnableToPerformAnswerId, "Unable to perform" },
                        { Answers.KedMemberPhysicallyUnableAnswerId, "Member physically unable" },
                        { Answers.EgfrNotesAnswerId, "Unable to perform notes." },
                        { Answers.DosAnswerId, DateTime.Now.ToString("O") }
                    },
                    Answers.KedMemberPhysicallyUnableAnswerId
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.KedNotPerformedAnswerId, "No" },
                        { Answers.KedMemberRefusedAnswerId, "Member refused" },
                        { Answers.KedMemberApprehensionAnswerId, "Member apprehension" },
                        { Answers.EgfrNotesAnswerId, "Member refused notes." },
                        { Answers.DosAnswerId, DateTime.Now.ToString("O") }
                    },
                    Answers.KedMemberApprehensionAnswerId
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.KedNotPerformedAnswerId, "No" },
                        { Answers.KedMemberRefusedAnswerId, "Member refused" },
                        { Answers.KedScheduledToCompleteAnswerId, "Scheduled to complete" },
                        { Answers.EgfrNotesAnswerId, "Member refused notes." },
                        { Answers.DosAnswerId, DateTime.Now.ToString("O") }
                    },
                    Answers.KedScheduledToCompleteAnswerId
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.KedNotPerformedAnswerId, "No" },
                        { Answers.KedMemberRefusedAnswerId, "Member refused" },
                        { Answers.KedNotInterestedAnswerId, "Not interested" },
                        { Answers.EgfrNotesAnswerId, "Member refused notes." },
                        { Answers.DosAnswerId, DateTime.Now.ToString("O") }
                    },
                    Answers.KedNotInterestedAnswerId
                ],
                [
                    new Dictionary<int, string>
                    {
                        { Answers.KedNotPerformedAnswerId, "No" },
                        { Answers.KedMemberRefusedAnswerId, "Member refused" },
                        { Answers.KedMemberRecentlyCompletedAnswerId, "Member recently completed" },
                        { Answers.EgfrNotesAnswerId, "Member refused notes." },
                        { Answers.DosAnswerId, DateTime.Now.ToString("O") }
                    },
                    Answers.KedMemberRecentlyCompletedAnswerId
                ]
            };
        }
    }

    #endregion
}