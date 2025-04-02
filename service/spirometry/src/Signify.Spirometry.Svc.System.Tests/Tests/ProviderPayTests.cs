using Signify.EvaluationsApi.Core.Values;
using Signify.Spirometry.Svc.System.Tests.Core.Constants;
using Signify.Spirometry.Svc.System.Tests.Core.Actions;
using Signify.QE.MSTest.Attributes;
using Signify.Dps.Test.Utilities.DataGen;
using Signify.QE.Core.Models.Appointment;
using SpiroEvents;
using Appointment = Signify.Spirometry.Core.ApiClients.AppointmentApi.Responses.Appointment;

namespace Signify.Spirometry.Svc.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class ProviderPayTests : ProviderPayActions
{
    [RetryableTestMethod]
    [DynamicData(nameof(GetSpiroTestData))]
    public async Task ANC_T644_ProviderPayable(Dictionary<int, string> answersDict)
    {
        // Arrange
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        // Assert ProviderPay table & ProviderPayEvent 
        await ValidateProviderPayable(evaluation.EvaluationId, member.MemberPlanId);
    }

    [RetryableTestMethod]
    [DynamicData(nameof(VariousNotPerformedAnswers))]
    public async Task ANC_T715_NotPerformed_ProviderNonPayable(Dictionary<int, string> answersDict)
    {
        // Arrange
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();

        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,
            CoreApiActions.GetEvaluationAnswerList(answersDict));

        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        // Assert ProviderPay table & ProviderPayEvent 
        await ValidateProviderNonPayable(evaluation.EvaluationId);
    }
    
    [RetryableTestMethod]
    [DynamicData(nameof(GetOverreadData))]
    public async Task ANC_T713_OverreadResults_ProviderPay(Dictionary<int, string> answersDict, string obstructionPerOverread)
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
            Fev1FvcRatio = 2.5m, //add m suffix to treat the value as decimal
            OverreadBy = "JohnDoe",
            ObstructionPerOverread = obstructionPerOverread,
            ReceivedDateTime = DateTime.Now,
        };
        CoreKafkaActions.PublishEvent("overread_spirometry",overreadEventValue,evaluation.EvaluationId.ToString(),"OverreadProcessed");
        await Task.Delay(5000);
        
        // Assert ProviderPay table & ProviderPayEvent 
        if (obstructionPerOverread == "INCONCLUSIVE")
        {
            await ValidateProviderNonPayable(evaluation.EvaluationId);
        }
        else
        {
            await ValidateProviderPayable(evaluation.EvaluationId, member.MemberPlanId);
        }
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
                    }
                },
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "A" },
                            { Answers.FVCAnswerId, "70" },
                            { Answers.FEV1AnswerId, "100" },
                            { Answers.FEV1FVCAnswerId, "0.7" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                        }
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "C" },
                            { Answers.FVCAnswerId, "70" },
                            { Answers.FEV1AnswerId, "100" },
                            { Answers.FEV1FVCAnswerId, "0.7" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                        }
                    ],
            };
        }
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
                        { Answers.SessionGradeIdAnswerId, "D" },
                        { Answers.FVCAnswerId, "70" },
                        { Answers.FEV1AnswerId, "70" },
                        { Answers.FEV1FVCAnswerId, "0.65" },
                        { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                    },
                        Answers.obstructionPerOverread = "YES",
                    
                },
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "E" },
                            { Answers.FVCAnswerId, "70" },
                            { Answers.FEV1AnswerId, "70" },
                            { Answers.FEV1FVCAnswerId, "0.65" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                        },
                        Answers.obstructionPerOverread = "NO",
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "F" },
                            { Answers.FVCAnswerId, "70" },
                            { Answers.FEV1AnswerId, "70" },
                            { Answers.FEV1FVCAnswerId, "0.65" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                        },
                        Answers.obstructionPerOverread = "YES",
                    ],
                    [
                        new Dictionary<int, string>
                        {
                            { Answers.PerformedYesAnswerId, "Yes" },
                            { Answers.SessionGradeIdAnswerId, "D" },
                            { Answers.FVCAnswerId, "70" },
                            { Answers.FEV1AnswerId, "70" },
                            { Answers.FEV1FVCAnswerId, "0.65" },
                            { Answers.DosAnswerId, DateTime.Now.ToString("yyyy-MM-dd") },
                        },
                        Answers.obstructionPerOverread = "INCONCLUSIVE",
                    ]
            };
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
                        { Answers.MemberRefusedAnswerId, Answers.MemberRefusedAnswer },
                        { Answers.MemberScheduledToCompleteAnswerId, "Scheduled to complete" }
                    }
                ],
            };
        }
    }
}