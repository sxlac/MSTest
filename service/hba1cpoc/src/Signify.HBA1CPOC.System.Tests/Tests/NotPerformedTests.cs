using Signify.QE.MSTest.Attributes;
using Signify.EvaluationsApi.Core.Values;
using Signify.HBA1CPOC.System.Tests.Core.Constants;

namespace Signify.HBA1CPOC.System.Tests.Tests;

[TestClass,TestCategory("regression"),TestCategory("prod_smoke")]
public class NotPerformedTests: NotPerformedActions
{
    public TestContext TestContext { get; set; }
    
    [RetryableTestMethod]
    [DynamicData(nameof(VariousNotPerformedAnswersUnable))]
    public async Task ANC_T313_Hba1cpocNotPerformedUnable(Dictionary<int, string> answersDict)
    {
        // Arrange
        var (member,appointment,evaluation) = await CoreApiActions.PrepareEvaluation();
        
        var  dateStamp = DateTimeOffset.UtcNow; 
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,CoreApiActions.GetEvaluationAnswerList(answersDict));
         
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        // Assert
        await Validate_Database_Records_And_KafkaEvents(evaluation.EvaluationId, answersDict, member.MemberPlanId);
    }
    
    [RetryableTestMethod]
    [DynamicData(nameof(VariousNotPerformedAnswersMemberRefused))]
    public async Task ANC_T314_Hba1cpocNotPerformedRefused(Dictionary<int, string> answersDict)
    {
        // Arrange
        var (member,appointment,evaluation) = await CoreApiActions.PrepareEvaluation();
        
        var  dateStamp = DateTimeOffset.UtcNow; 
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,CoreApiActions.GetEvaluationAnswerList(answersDict));
         
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        // Assert
        await Validate_Database_Records_And_KafkaEvents(evaluation.EvaluationId, answersDict, member.MemberPlanId);
    }
    
    #region TestData

        private static IEnumerable<object[]> VariousNotPerformedAnswersUnable
        {
            get
            {
                return new[]
                {
                    new object[]
                    {
                        new Dictionary<int, string>
                        {
                            { Answers.MemberUnableNotesAnswerId, Answers.ProviderUnableNotesAnswer },
                            { Answers.Qid91483NoAnswerId, "No" },
                            { Answers.ProviderReasonAnswerId, Answers.ProviderUnableAnswer },
                            { NotPerformedReasons.TechnicalIssue.AnswerId, NotPerformedReasons.TechnicalIssue.Reason }
                        }
                    },
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.MemberUnableNotesAnswerId, Answers.ProviderUnableNotesAnswer },
                            { Answers.Qid91483NoAnswerId, "No" },
                            { Answers.ProviderReasonAnswerId, Answers.ProviderUnableAnswer },
                            {
                                NotPerformedReasons.EnvironmentalIssue.AnswerId,
                                NotPerformedReasons.EnvironmentalIssue.Reason
                            }
                        }
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.MemberUnableNotesAnswerId, Answers.ProviderUnableNotesAnswer },
                            { Answers.Qid91483NoAnswerId, "No" },
                            { Answers.ProviderReasonAnswerId, Answers.ProviderUnableAnswer },
                            {
                                NotPerformedReasons.NoSuppliesOrEquipment.AnswerId,
                                NotPerformedReasons.NoSuppliesOrEquipment.Reason
                            }
                        }
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.MemberUnableNotesAnswerId, Answers.ProviderUnableNotesAnswer },
                            { Answers.Qid91483NoAnswerId, "No" },
                            { Answers.ProviderReasonAnswerId, Answers.ProviderUnableAnswer },
                            {
                                NotPerformedReasons.InsufficientTraining.AnswerId,
                                NotPerformedReasons.InsufficientTraining.Reason
                            }
                        }
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.MemberUnableNotesAnswerId, Answers.ProviderUnableNotesAnswer },
                            { Answers.Qid91483NoAnswerId, "No" },
                            { Answers.ProviderReasonAnswerId, Answers.ProviderUnableAnswer },
                            { NotPerformedReasons.MemberUnable.AnswerId, NotPerformedReasons.MemberUnable.Reason }
                        }
                    ]
                };
            }
        }
        
        private static IEnumerable<object[]> VariousNotPerformedAnswersMemberRefused
        {
            get
            {
                return new[]
                {
                    new object[]
                    {
                        new Dictionary<int, string>
                        {
                            { Answers.MemberRefusedNotesAnswerId, Answers.MemberRefusedNotesAnswer },
                            { Answers.Qid91483NoAnswerId, "No" },
                            { Answers.MemberReasonAnswerId, Answers.MemberRefusedAnswer },
                            { NotPerformedReasons.RecentlyCompleted.AnswerId, NotPerformedReasons.RecentlyCompleted.Reason }
                        }
                    },
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.MemberRefusedNotesAnswerId, Answers.MemberRefusedNotesAnswer },
                            { Answers.Qid91483NoAnswerId, "No" },
                            { Answers.MemberReasonAnswerId, Answers.MemberRefusedAnswer },
                            { NotPerformedReasons.ScheduledToComplete.AnswerId, NotPerformedReasons.ScheduledToComplete.Reason }
                        }
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.MemberRefusedNotesAnswerId, Answers.MemberRefusedNotesAnswer },
                            { Answers.Qid91483NoAnswerId, "No" },
                            { Answers.MemberReasonAnswerId, Answers.MemberRefusedAnswer },
                            { NotPerformedReasons.MemberApprehension.AnswerId, NotPerformedReasons.MemberApprehension.Reason }
                        }
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.MemberRefusedNotesAnswerId, Answers.MemberRefusedNotesAnswer },
                            { Answers.Qid91483NoAnswerId, "No" },
                            { Answers.MemberReasonAnswerId, Answers.MemberRefusedAnswer },
                            { NotPerformedReasons.NotInterested.AnswerId, NotPerformedReasons.NotInterested.Reason }
                        }
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.MemberRefusedNotesAnswerId, Answers.MemberRefusedNotesAnswer },
                            { Answers.Qid91483NoAnswerId, "No" },
                            { Answers.MemberReasonAnswerId, Answers.MemberRefusedAnswer },
                            { NotPerformedReasons.Other.AnswerId, NotPerformedReasons.Other.Reason }
                        }
                    ]
                };
            }
        }

    #endregion
}