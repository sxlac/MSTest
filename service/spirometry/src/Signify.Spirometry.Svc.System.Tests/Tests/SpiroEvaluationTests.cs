using Signify.EvaluationsApi.Core.Values;
using Signify.Spirometry.Svc.System.Tests.Core.Constants;
using Signify.Spirometry.Svc.System.Tests.Core.Actions;
using Signify.QE.MSTest.Attributes;
using ResultsReceived = Signify.Spirometry.Core.Events.Akka.ResultsReceived;

namespace Signify.Spirometry.Svc.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class SpiroEvaluationTests : PerformedActions
{
    [RetryableTestMethod]
    [DynamicData(nameof(GetSpiroTestData))]
    public async Task ANC_T397_FrequencyTypeAnswers(Dictionary<int, string> answersDict)
    {
        // Arrange
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        // Assert
        // Database SpirometryExamResults table
        var spiro = await GetSpiroFrequencyResultsByEvaluationId(evaluation.EvaluationId);
        Assert.AreEqual(Convert.ToInt32(answersDict[Answers.LungFunctionScoreAnswerId]), spiro.LungFunctionScore);
        
        // Kafka results event
        var results = await CoreKafkaActions.GetSpiroResultsReceivedEvent<ResultsReceived>(evaluation.EvaluationId);
        Assert.AreEqual(Convert.ToInt32(answersDict[Answers.LungFunctionScoreAnswerId]), results.Results.LungFunctionScore);
        
        // Validate Frequency value in both database and Kafka
        ValidateResults(answersDict, spiro,results);

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
                        { Answers.FVCAnswerId, "80" },
                        { Answers.FEV1AnswerId, "80" },
                        { Answers.FEV1FVCAnswerId, "0.65" },
                        { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                        { Answers.NeverCoughAnswerId, "Never" },
                        { Answers.NeverWheezyChestAnswerId, "Never" },
                        { Answers.NeverBreathShortnessAnswerId, "Never" },
                        { Answers.LungFunctionScoreAnswerId, "1" },
                    }
                },
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "B" },
                            { Answers.FVCAnswerId, "80" },
                            { Answers.FEV1AnswerId, "80" },
                            { Answers.FEV1FVCAnswerId, "0.65" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                            { Answers.RarelyCoughAnswerId, "Rarely" },
                            { Answers.RarelyWheezyChestAnswerId, "Rarely" },
                            { Answers.RarelyBreathShortnessAnswerId, "Rarely" },
                            { Answers.LungFunctionScoreAnswerId, "10" },
                        }
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "B" },
                            { Answers.FVCAnswerId, "80" },
                            { Answers.FEV1AnswerId, "80" },
                            { Answers.FEV1FVCAnswerId, "0.65" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                            { Answers.SometimesCoughAnswerId, "Sometimes" },
                            { Answers.SometimesWheezyChestAnswerId, "Sometimes" },
                            { Answers.SometimesBreathShortnessAnswerId, "Sometimes" },
                            { Answers.LungFunctionScoreAnswerId, "21" },
                        }
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "B" },
                            { Answers.FVCAnswerId, "80" },
                            { Answers.FEV1AnswerId, "80" },
                            { Answers.FEV1FVCAnswerId, "0.65" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                            { Answers.OftenCoughAnswerId, "Often" },
                            { Answers.OftenWheezyChestAnswerId, "Often" },
                            { Answers.OftenBreathShortnessAnswerId, "Often" },
                            { Answers.LungFunctionScoreAnswerId, "8" },
                        }
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "B" },
                            { Answers.FVCAnswerId, "80" },
                            { Answers.FEV1AnswerId, "80" },
                            { Answers.FEV1FVCAnswerId, "0.65" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                            { Answers.VeryOftenCoughAnswerId, "Very often" },
                            { Answers.VeryOftenWheezyChestAnswerId, "Very often" },
                            { Answers.VeryOftenBreathShortnessAnswerId, "Very often" },
                            { Answers.LungFunctionScoreAnswerId, "8" },
                        }
                    ]
            };
            
        }
    }

    [RetryableTestMethod]
    [DynamicData(nameof(GetTrileanTypeTestData))]
    public async Task ANC_T398_TrileanTypeAnswers(Dictionary<int, string> answersDict)
    {
        // Arrange
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        // Assert
        // Database SpirometryExamResults table
        var spiro = await GetSpiroTrileanTypeResultsByEvaluationId(evaluation.EvaluationId);
        
        Assert.AreEqual(Convert.ToInt32(answersDict[Answers.SmokingYearsAnswerId]), spiro.TotalYearsSmoking);
        Assert.AreEqual(answersDict.ContainsKey(Answers.HasSmokedTrueAnswerId), spiro.HasSmokedTobacco);
        Assert.AreEqual(answersDict.ContainsKey(Answers.ProduceSputumYesAnswerId), spiro.ProducesSputumWithCough);
        
        //  Kafka results event
        var results = await CoreKafkaActions.GetSpiroResultsReceivedEvent<ResultsReceived>(evaluation.EvaluationId);
        
        Assert.AreEqual(Convert.ToInt32(answersDict[Answers.SmokingYearsAnswerId]), results.Results.TotalYearsSmoking);
        
        // Validate Trilean Type value in both database and Kafka
        ValidateResults(answersDict, spiro,results);

    }
    
     private static IEnumerable<object[]> GetTrileanTypeTestData
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
                        { Answers.FVCAnswerId, "80" },
                        { Answers.FEV1AnswerId, "80" },
                        { Answers.FEV1FVCAnswerId, "0.65" },
                        { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                        { Answers.HasSmokedTrueAnswerId, "1" },
                        { Answers.SmokingYearsAnswerId, "15" },
                        { Answers.ProduceSputumYesAnswerId, "1" },
                        { Answers.HadWheezingYesAnswerId, "1" },
                        { Answers.ShortBreathatRestYesAnswerId, "1" },
                        { Answers.ShortBreathExertionYesAnswerId, "1" },
                    }
                },
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "B" },
                            { Answers.FVCAnswerId, "80" },
                            { Answers.FEV1AnswerId, "80" },
                            { Answers.FEV1FVCAnswerId, "0.65" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                            { Answers.HasSmokedNoAnswerId, "1" },
                            { Answers.SmokingYearsAnswerId, "15" },
                            { Answers.ProduceSputumYesAnswerId, "1" },
                            { Answers.HadWheezingUnknownAnswerId, "1" },
                            { Answers.ShortBreathatRestUnknownAnswerId, "1" },
                            { Answers.ShortBreathExertionUnknownAnswerId, "1" },
                        }
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "B" },
                            { Answers.FVCAnswerId, "80" },
                            { Answers.FEV1AnswerId, "80" },
                            { Answers.FEV1FVCAnswerId, "0.65" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                            { Answers.HasSmokedTrueAnswerId, "1" },
                            { Answers.SmokingYearsAnswerId, "20" },
                            { Answers.ProduceSputumNoAnswerId, "1" },
                            { Answers.HadWheezingNoAnswerId, "1" },
                            { Answers.ShortBreathatRestNoAnswerId, "1" },
                            { Answers.ShortBreathExertionNoAnswerId, "1" },
                        }
                    ]
                   
            };
            
        }
    }

}