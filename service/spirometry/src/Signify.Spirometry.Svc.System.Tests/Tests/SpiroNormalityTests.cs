using Signify.EvaluationsApi.Core.Values;
using Signify.Spirometry.Svc.System.Tests.Core.Constants;
using Signify.Spirometry.Svc.System.Tests.Core.Actions;
using Signify.QE.MSTest.Attributes;

namespace Signify.Spirometry.Svc.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class SpiroNormalityTests : PerformedActions
{
    [RetryableTestMethod]
    [DynamicData(nameof(GetSpiroTestData))]
    public async Task ANC_T711_Performed(Dictionary<int, string> answersDict, string normality)
    {
        // Arrange
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        // Assert
        // Database SpirometryExamResults table
        var spiro = await GetSpiroResultsByEvaluationId(evaluation.EvaluationId);
        
        Assert.AreEqual(normality, spiro.Normality);
        Assert.AreEqual(answersDict[Answers.HistoryCOPDAnswerId] == "Chronic obstructive pulmonary disease (COPD)", spiro.HasHistoryOfCopd);
    }
     private static IEnumerable<object[]> GetSpiroTestData
    {
        get
        {
            return new[]
            {
                new object[]
                {
                    new Dictionary<int, string>
                    {
                        { Answers.PerformedYesAnswerId, "Yes" },
                        { Answers.SessionGradeIdAnswerId, "B" },
                        { Answers.FVCAnswerId, "70" },
                        { Answers.FEV1AnswerId, "70" },
                        { Answers.FEV1FVCAnswerId, "0.65" },
                        { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                        { Answers.NeverCoughAnswerId, "Never" },
                        { Answers.NeverWheezyChestAnswerId, "Never" },
                        { Answers.NeverBreathShortnessAnswerId, "Never" },
                        { Answers.LungFunctionScoreAnswerId, "1" },
                        { Answers.HistoryCOPDAnswerId, "Chronic obstructive pulmonary disease (COPD)" }
                    },
                        Answers.Normality = "Abnormal"
                    
                },
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "A" },
                            { Answers.FVCAnswerId, "80" },
                            { Answers.FEV1AnswerId, "80" },
                            { Answers.FEV1FVCAnswerId, "0.7" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                            { Answers.NeverCoughAnswerId, "Never" },
                            { Answers.NeverWheezyChestAnswerId, "Never" },
                            { Answers.NeverBreathShortnessAnswerId, "Never" },
                            { Answers.LungFunctionScoreAnswerId, "1" },
                            { Answers.HistoryCOPDAnswerId, "Chronic obstructive pulmonary disease (COPD)" }
                        },
                        Answers.Normality = "Normal"

                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "C" },
                            { Answers.FVCAnswerId, "80" },
                            { Answers.FEV1AnswerId, "80" },
                            { Answers.FEV1FVCAnswerId, "0.85" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                            { Answers.NeverCoughAnswerId, "Never" },
                            { Answers.NeverWheezyChestAnswerId, "Never" },
                            { Answers.NeverBreathShortnessAnswerId, "Never" },
                            { Answers.LungFunctionScoreAnswerId, "1" },
                            { Answers.HistoryCOPDAnswerId, "Chronic obstructive pulmonary disease (COPD)" }
                        },
                        Answers.Normality = "Normal"
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "C" },
                            { Answers.FVCAnswerId, "80" },
                            { Answers.FEV1AnswerId, "80" },
                            { Answers.FEV1FVCAnswerId, "0.85" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                            { Answers.NeverCoughAnswerId, "Never" },
                            { Answers.NeverWheezyChestAnswerId, "Never" },
                            { Answers.NeverBreathShortnessAnswerId, "Never" },
                            { Answers.LungFunctionScoreAnswerId, "1" },
                            { Answers.HistoryCOPDAnswerId, "Hypertension" }
                        },
                        Answers.Normality = "Normal"
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "C" },
                            { Answers.FVCAnswerId, "80" },
                            { Answers.FEV1AnswerId, "80" },
                            { Answers.FEV1FVCAnswerId, "0.85" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                            { Answers.NeverCoughAnswerId, "Never" },
                            { Answers.NeverWheezyChestAnswerId, "Never" },
                            { Answers.NeverBreathShortnessAnswerId, "Never" },
                            { Answers.LungFunctionScoreAnswerId, "1" },
                            { Answers.HistoryCOPDAnswerId, "Diabetes with neuropathy" }
                        },
                        Answers.Normality = "Normal"
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "C" },
                            { Answers.FVCAnswerId, "80" },
                            { Answers.FEV1AnswerId, "80" },
                            { Answers.FEV1FVCAnswerId, "0.85" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                            { Answers.NeverCoughAnswerId, "Never" },
                            { Answers.NeverWheezyChestAnswerId, "Never" },
                            { Answers.NeverBreathShortnessAnswerId, "Never" },
                            { Answers.LungFunctionScoreAnswerId, "1" },
                            { Answers.HistoryCOPDAnswerId, "Constipation" }
                        },
                        Answers.Normality = "Normal"
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "C" },
                            { Answers.FVCAnswerId, "80" },
                            { Answers.FEV1AnswerId, "80" },
                            { Answers.FEV1FVCAnswerId, "0.85" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                            { Answers.NeverCoughAnswerId, "Never" },
                            { Answers.NeverWheezyChestAnswerId, "Never" },
                            { Answers.NeverBreathShortnessAnswerId, "Never" },
                            { Answers.LungFunctionScoreAnswerId, "1" },
                            { Answers.HistoryCOPDAnswerId, "Heart failure" }
                        },
                        Answers.Normality = "Normal"
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "C" },
                            { Answers.FVCAnswerId, "80" },
                            { Answers.FEV1AnswerId, "80" },
                            { Answers.FEV1FVCAnswerId, "0.85" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                            { Answers.NeverCoughAnswerId, "Never" },
                            { Answers.NeverWheezyChestAnswerId, "Never" },
                            { Answers.NeverBreathShortnessAnswerId, "Never" },
                            { Answers.LungFunctionScoreAnswerId, "1" },
                            { Answers.HistoryCOPDAnswerId, "Orthostatic hypotension" }
                        },
                        Answers.Normality = "Normal"
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "C" },
                            { Answers.FVCAnswerId, "80" },
                            { Answers.FEV1AnswerId, "80" },
                            { Answers.FEV1FVCAnswerId, "0.85" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                            { Answers.NeverCoughAnswerId, "Never" },
                            { Answers.NeverWheezyChestAnswerId, "Never" },
                            { Answers.NeverBreathShortnessAnswerId, "Never" },
                            { Answers.LungFunctionScoreAnswerId, "1" },
                            { Answers.HistoryCOPDAnswerId, "Seasonal allergic rhinitis" }
                        },
                        Answers.Normality = "Normal"
                    ]
            };
            
        }
    }

}