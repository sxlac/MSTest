using Signify.EvaluationsApi.Core.Values;
using Signify.Spirometry.Svc.System.Tests.Core.Constants;
using Signify.Spirometry.Svc.System.Tests.Core.Actions;
using Signify.QE.MSTest.Attributes;
using Signify.Dps.Test.Utilities.DataGen;
using ResultsReceived = Signify.Spirometry.Core.Events.Akka.ResultsReceived;
using SpiroEvents;

namespace Signify.Spirometry.Svc.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class DxLoopbackTests : BaseTestActions
{

    [RetryableTestMethod]
    [DynamicData(nameof(GetOverreadData))]
    public async Task ANC_T519_OverreadResults_HistoryofCOPD(Dictionary<int, string> answersDict,
        string obstructionPerOverread, bool IsBillable, bool NeedsFlag, string Normality)
    {
        // Arrange
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();

        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,
            CoreApiActions.GetEvaluationAnswerList(answersDict));

        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);

        //publish overread event
        var overreadEventValue = new OverreadProcessed()
        {
            OverreadId = DataGen.NewGuid(),
            MemberId = DataGen.RandomInt(0, 100000),
            AppointmentId = appointment.AppointmentId,
            SessionId = DataGen.NewGuid(),
            PerformedDateTime = DateTime.Now,
            OverreadDateTime = DateTime.Now,
            BestTestId = DataGen.NewGuid(),
            BestFvcTestId = DataGen.NewGuid(),
            BestFvcTestComment = "",
            BestFev1TestId = DataGen.NewGuid(),
            BestFev1TestComment = "",
            BestPefTestId = DataGen.NewGuid(),
            BestPefTestComment = "",
            Comment = "TestComment",
            Fev1FvcRatio = 0.7m, //add m suffix to treat the value as decimal
            OverreadBy = "JohnDoe",
            ObstructionPerOverread = obstructionPerOverread,
            ReceivedDateTime = DateTime.Now,
        };
        CoreKafkaActions.PublishEvent("overread_spirometry", overreadEventValue, evaluation.EvaluationId.ToString(),
            "OverreadProcessed");
        await Task.Delay(5000);


        // Validate NeedFlags value in Evaluation_saga tb
        var spiro = await getEvalSagaByEvaluationId(evaluation.EvaluationId);
        Assert.AreEqual(NeedsFlag, spiro.ClinicalSupportData.NeedsFlag);

        // Validate Status Codes in Database
        var exam = await getSpiroExamByEvaluationId(evaluation.EvaluationId);
        var expectedIds = new List<int>
        {
            ExamStatusCodes.ExamPerformed.SpiroStatusCodeId,
            ExamStatusCodes.ResultsReceived.SpiroStatusCodeId,
            ExamStatusCodes.OverreadProcessed.SpiroStatusCodeId,
        };
        await ValidateExamStatusCodesByExamId(exam.SpirometryExamId, expectedIds, 10, 3);

        //  Kafka results event
        var results = await CoreKafkaActions.GetSpiroResultsReceivedEvent<ResultsReceived>(evaluation.EvaluationId);
        
        Assert.AreEqual(Convert.ToInt32(answersDict[Answers.LungFunctionScoreAnswerId]), results.Results.LungFunctionScore);
        Assert.AreEqual(overreadEventValue.Fev1FvcRatio, results.Results.Fev1OverFvc);
        Assert.AreEqual(answersDict[Answers.SessionGradeIdAnswerId], results.Results.SessionGrade);
        Assert.AreEqual(Convert.ToInt32(answersDict[Answers.FVCAnswerId]), results.Results.Fvc);
        Assert.AreEqual(Convert.ToInt32(answersDict[Answers.FEV1AnswerId]), results.Results.Fev1);
        Assert.AreEqual("N", results.Results.FvcNormality);
        Assert.AreEqual("A", results.Results.Fev1Normality);
        Assert.IsNull(results.Results.Copd);
        Assert.AreEqual(evaluation.EvaluationId, results.EvaluationId);
        Assert.AreEqual(IsBillable, results.IsBillable);
        Assert.AreEqual(Normality, results.Determination);
        
    }
    [RetryableTestMethod]
    [DynamicData(nameof(GetDxCOPDdData))]
    public async Task ANC_T509_OverreadResults_DxCOPDAsserted(Dictionary<int, string> answersDict,
        string obstructionPerOverread, bool IsBillable, string Normality)
    {
        // Arrange
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();

        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,
            CoreApiActions.GetEvaluationAnswerList(answersDict));

        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);

        //publish overread event
        var overreadEventValue = new OverreadProcessed()
        {
            OverreadId = DataGen.NewGuid(),
            MemberId = DataGen.RandomInt(0, 100000),
            AppointmentId = appointment.AppointmentId,
            SessionId = DataGen.NewGuid(),
            PerformedDateTime = DateTime.Now,
            OverreadDateTime = DateTime.Now,
            BestTestId = DataGen.NewGuid(),
            BestFvcTestId = DataGen.NewGuid(),
            BestFvcTestComment = "",
            BestFev1TestId = DataGen.NewGuid(),
            BestFev1TestComment = "",
            BestPefTestId = DataGen.NewGuid(),
            BestPefTestComment = "",
            Comment = "TestComment",
            Fev1FvcRatio = 0.7m, //add m suffix to treat the value as decimal
            OverreadBy = "JohnDoe",
            ObstructionPerOverread = obstructionPerOverread,
            ReceivedDateTime = DateTime.Now,
        };
        CoreKafkaActions.PublishEvent("overread_spirometry", overreadEventValue, evaluation.EvaluationId.ToString(),
            "OverreadProcessed");
        await Task.Delay(5000);


        // Validate NeedFlags value in Evaluation_saga tb
        var spiro = await getEvalSagaByEvaluationId(evaluation.EvaluationId);
        Assert.IsFalse(spiro.ClinicalSupportData.NeedsFlag);

        // Validate Status Codes in Database
        var exam = await getSpiroExamByEvaluationId(evaluation.EvaluationId);
        var expectedIds = new List<int>
        {
            ExamStatusCodes.ExamPerformed.SpiroStatusCodeId,
            ExamStatusCodes.ResultsReceived.SpiroStatusCodeId,
            ExamStatusCodes.OverreadProcessed.SpiroStatusCodeId,
        };
        await ValidateExamStatusCodesByExamId(exam.SpirometryExamId, expectedIds, 10, 3);

        //  Kafka results event
        var results = await CoreKafkaActions.GetSpiroResultsReceivedEvent<ResultsReceived>(evaluation.EvaluationId);
        
        Assert.AreEqual(Convert.ToInt32(answersDict[Answers.LungFunctionScoreAnswerId]), results.Results.LungFunctionScore);
        Assert.AreEqual(overreadEventValue.Fev1FvcRatio, results.Results.Fev1OverFvc);
        Assert.AreEqual(answersDict[Answers.SessionGradeIdAnswerId], results.Results.SessionGrade);
        Assert.AreEqual(Convert.ToInt32(answersDict[Answers.FVCAnswerId]), results.Results.Fvc);
        Assert.AreEqual(Convert.ToInt32(answersDict[Answers.FEV1AnswerId]), results.Results.Fev1);
        Assert.AreEqual("N", results.Results.FvcNormality);
        Assert.AreEqual("A", results.Results.Fev1Normality);
        Assert.IsTrue(results.Results.Copd);
        Assert.AreEqual(evaluation.EvaluationId, results.EvaluationId);
        Assert.AreEqual(IsBillable, results.IsBillable);
        Assert.AreEqual(Normality, results.Determination);
        
    }
    private static IEnumerable<object[]> GetOverreadData
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
                        { Answers.SessionGradeIdAnswerId, "E" },
                        { Answers.FVCAnswerId, "100" },
                        { Answers.FEV1AnswerId, "30" },
                        { Answers.FEV1FVCAnswerId, "0.65" },
                        { Answers.HistoryCOPDAnswerId, "Chronic obstructive pulmonary disease (COPD)" },
                        { Answers.LungFunctionScoreAnswerId, "19" },
                        { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                    },
                        Answers.obstructionPerOverread = "YES",
                        Answers.IsBillable =true,
                        Answers.NeedsFlag =true,
                        Answers.Normality = "A",
                    
                },
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "E" },
                            { Answers.FVCAnswerId, "100" },
                            { Answers.FEV1AnswerId, "30" },
                            { Answers.FEV1FVCAnswerId, "0.65" },
                            { Answers.HistoryCOPDAnswerId, "Chronic obstructive pulmonary disease" },
                            { Answers.LungFunctionScoreAnswerId, "19" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                        },
                        Answers.obstructionPerOverread = "YES",
                        Answers.IsBillable =true,
                        Answers.NeedsFlag =true,
                        Answers.Normality = "A",
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "E" },
                            { Answers.FVCAnswerId, "100" },
                            { Answers.FEV1AnswerId, "30" },
                            { Answers.FEV1FVCAnswerId, "0.65" },
                            { Answers.HistoryCOPDAnswerId, "COPD" },
                            { Answers.LungFunctionScoreAnswerId, "19" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                        },
                        Answers.obstructionPerOverread = "YES",
                        Answers.IsBillable =true,
                        Answers.NeedsFlag =true,
                        Answers.Normality = "A",
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "E" },
                            { Answers.FVCAnswerId, "100" },
                            { Answers.FEV1AnswerId, "30" },
                            { Answers.FEV1FVCAnswerId, "0.65" },
                            { Answers.HistoryCOPDAnswerId, "Chronic obstructive pulmonary disease (COPD)" },
                            { Answers.LungFunctionScoreAnswerId, "17" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                        },
                        Answers.obstructionPerOverread = "YES",
                        Answers.IsBillable =true,
                        Answers.NeedsFlag =true,
                        Answers.Normality = "A",
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "F" },
                            { Answers.FVCAnswerId, "100" },
                            { Answers.FEV1AnswerId, "30" },
                            { Answers.FEV1FVCAnswerId, "0.65" },
                            { Answers.HistoryCOPDAnswerId, "Chronic obstructive pulmonary disease (COPD)" },
                            { Answers.LungFunctionScoreAnswerId, "15" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                        },
                        Answers.obstructionPerOverread = "NO",
                        Answers.IsBillable =true,
                        Answers.NeedsFlag =false,
                        Answers.Normality = "N",
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "D" },
                            { Answers.FVCAnswerId, "100" },
                            { Answers.FEV1AnswerId, "30" },
                            { Answers.FEV1FVCAnswerId, "0.65" },
                            { Answers.HistoryCOPDAnswerId, "Chronic obstructive pulmonary disease (COPD)" },
                            { Answers.LungFunctionScoreAnswerId, "15" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                        },
                        Answers.obstructionPerOverread = "INCONCLUSIVE",
                        Answers.IsBillable =false,
                        Answers.NeedsFlag =false,
                        Answers.Normality = "U",
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "D" },
                            { Answers.FVCAnswerId, "100" },
                            { Answers.FEV1AnswerId, "30" },
                            { Answers.FEV1FVCAnswerId, "0.5" },
                            { Answers.HistoryCOPDAnswerId, "Hypertension" },
                            { Answers.LungFunctionScoreAnswerId, "15" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                        },
                        Answers.obstructionPerOverread = "YES",
                        Answers.IsBillable =true,
                        Answers.NeedsFlag =true,
                        Answers.Normality = "A",
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "D" },
                            { Answers.FVCAnswerId, "100" },
                            { Answers.FEV1AnswerId, "30" },
                            { Answers.FEV1FVCAnswerId, "0.5" },
                            { Answers.HistoryCOPDAnswerId, "Hypertension" },
                            { Answers.LungFunctionScoreAnswerId, "15" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                        },
                        Answers.obstructionPerOverread = "NO",
                        Answers.IsBillable = true ,
                        Answers.NeedsFlag =false ,
                        Answers.Normality = "N",
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "D" },
                            { Answers.FVCAnswerId, "100" },
                            { Answers.FEV1AnswerId, "30" },
                            { Answers.FEV1FVCAnswerId, "0.5" },
                            { Answers.HistoryCOPDAnswerId, "Hypertension" },
                            { Answers.LungFunctionScoreAnswerId, "15" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                        },
                        Answers.obstructionPerOverread = "INCONCLUSIVE",
                        Answers.IsBillable =false,
                        Answers.NeedsFlag =false ,
                        Answers.Normality = "U",
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "D" },
                            { Answers.FVCAnswerId, "100" },
                            { Answers.FEV1AnswerId, "30" },
                            { Answers.FEV1FVCAnswerId, "0.5" },
                            { Answers.HistoryCOPDAnswerId, "Chronic obstructive pulmonary disease (COPD)"},
                            { Answers.LungFunctionScoreAnswerId, "19" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                        },
                        Answers.obstructionPerOverread = "INCONCLUSIVE",
                        Answers.IsBillable =false,
                        Answers.NeedsFlag =false ,
                        Answers.Normality = "U",
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "D" },
                            { Answers.FVCAnswerId, "100" },
                            { Answers.FEV1AnswerId, "30" },
                            { Answers.FEV1FVCAnswerId, "0.5" },
                            { Answers.HistoryCOPDAnswerId, "Chronic obstructive pulmonary disease (COPD)" },
                            { Answers.LungFunctionScoreAnswerId, "19" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                        },
                        Answers.obstructionPerOverread = "NO",
                        Answers.IsBillable =true,
                        Answers.NeedsFlag =false ,
                        Answers.Normality = "N",
                    ],
                    
                 
            };
        }
    }
     private static IEnumerable<object[]> GetDxCOPDdData
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
                        { Answers.SessionGradeIdAnswerId, "E" },
                        { Answers.FVCAnswerId, "100" },
                        { Answers.FEV1AnswerId, "30" },
                        { Answers.FEV1FVCAnswerId, "0.3" },
                        { Answers.DxCOPDAnswerId, "YES" },
                        { Answers.LungFunctionScoreAnswerId, "19" },
                        { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                    },
                        Answers.obstructionPerOverread = "YES",
                        Answers.IsBillable =true,
                        Answers.Normality = "A",
                    
                },
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "E" },
                            { Answers.FVCAnswerId, "100" },
                            { Answers.FEV1AnswerId, "30" },
                            { Answers.FEV1FVCAnswerId, "0.3" },
                            { Answers.DxCOPDAnswerId, "YES" },
                            { Answers.LungFunctionScoreAnswerId, "19" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                        },
                        Answers.obstructionPerOverread = "NO",
                        Answers.IsBillable = true,
                        Answers.Normality = "N",
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "E" },
                            { Answers.FVCAnswerId, "100" },
                            { Answers.FEV1AnswerId, "30" },
                            { Answers.FEV1FVCAnswerId, "0.3" },
                            { Answers.DxCOPDAnswerId, "YES" },
                            { Answers.LungFunctionScoreAnswerId, "19" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                        },
                        Answers.obstructionPerOverread = "INCONCLUSIVE",
                        Answers.IsBillable = false,
                        Answers.Normality = "U",
                    ],
                 
            };
        }
    }
 
}