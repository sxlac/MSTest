using Newtonsoft.Json.Linq;
using Signify.EvaluationsApi.Core.Values;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.System.Tests.Core.Constants;
using Signify.PAD.Svc.System.Tests.Core.Models.Kafka;

namespace Signify.PAD.Svc.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class PerformedTests : PerformedActions
{
    
    [RetryableTestMethod]
    [DynamicData(nameof(GetNormalityTestData))]
    public async Task ANC_T372_Performed(string leftValue, string rightValue, string lNormality, string rNormality, string determination, string lSeverity, string rSeverity)
    {
        // Arrange
        var (member, appointment, evaluation) = await CoreApiActions.PrepareEvaluation();
        var answersDict = GeneratePerformedAnswers(leftValue,rightValue);
        
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId, CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);
        
        // Assert
        // Database PAD table
        var pad = await GetPadByEvaluationId(evaluation.EvaluationId, 15, 3);
        pad.ApplicationId.Should().Be(TestConstants.ApplicationId);
        pad.MemberPlanId.Should().Be(Convert.ToInt32(member.MemberPlanId));
        pad.MemberId.Should().Be(Convert.ToInt32(member.MemberId));
        pad.AppointmentId.Should().Be(appointment.AppointmentId);
        pad.ProviderId.Should().Be(Provider.ProviderId);
        pad.CenseoId.Should().Be(member.CenseoId);
        pad.ClientId.Should().Be(member.ClientID);
        pad.City.Should().Be(member.City);
        pad.State.Should().Be(member.State);
        pad.AddressLineOne.Should().Be(member.AddressLineOne);
        pad.AddressLineTwo.Should().Be(member.AddressLineTwo);
        pad.ZipCode.Should().Be(member.ZipCode);
        pad.NationalProviderIdentifier.Should().Be(Provider.NationalProviderIdentifier);
        pad.FirstName.Should().Be(member.FirstName);
        pad.LastName.Should().Be(member.LastName);
        pad.MiddleName.Should().Be(member.MiddleName);
        pad.LeftNormalityIndicator.Should().Be(lNormality);
        pad.LeftScoreAnswerValue.Should().Be(leftValue);
        pad.LeftSeverityAnswerValue.Should().Be(lSeverity);
        pad.RightNormalityIndicator.Should().Be(rNormality);
        pad.RightScoreAnswerValue.Should().Be(rightValue);
        pad.RightSeverityAnswerValue.Should().Be(rSeverity);
        pad.DateOfBirth.Should().Be(member.DateOfBirth);
        pad.DateOfService.Should().Be(DateTime.Parse(answersDict[Answers.DosAnswerId]).Date);
        
        // Database status updates
        var expectedIds = new List<int>
        {
            ExamStatusCodes.ExamPerformed.PADStatusCodeId
        };
        
        var finalizedEvent =
            await CoreKafkaActions.GetEvaluationEvent<EvaluationFinalizedEvent>(evaluation.EvaluationId,
                "EvaluationFinalizedEvent");
        await ValidateExamStatusCodesByEvaluationId(evaluation.EvaluationId, expectedIds, 10, 3);
        
        // Kafka performed status event
        var performed = await CoreKafkaActions.GetPerformedStatusEvent<PerformedEvent>(evaluation.EvaluationId);
        performed.MemberPlanId.Should().Be(member.MemberPlanId);
        performed.ProviderId.Should().Be(Provider.ProviderId);
        performed.CreateDate.Should().Be(finalizedEvent.CreatedDateTime);
        performed.ReceivedDate.Should().Be(finalizedEvent.ReceivedDateTime);
        performed.ProductCode.Should().Be(TestConstants.Product);
        
        // Kafka results event
        var results = await CoreKafkaActions.GetExamResultEvent<ResultsReceived>(evaluation.EvaluationId);
        results.Results[0].Result.Should().Be(results.Results[0].Side=="L"?leftValue:rightValue);
        results.Results[1].Result.Should().Be(results.Results[1].Side=="L"?leftValue:rightValue);
        results.Results[0].AbnormalIndicator.Should().Be(results.Results[0].Side=="L"?lNormality:rNormality);
        results.Results[1].AbnormalIndicator.Should().Be(results.Results[1].Side=="L"?lNormality:rNormality);
        results.Results[0].Severity.Should().Be(results.Results[0].Side=="L"?lSeverity:rSeverity);
        results.Results[1].Severity.Should().Be(results.Results[1].Side=="L"?lSeverity:rSeverity);
        results.Determination.Should().Be(determination);
        results.ProductCode.Should().Be(TestConstants.Product);
        results.PerformedDate.UtcDateTime.Should().BeCloseTo(DateTimeOffset.Parse(answersDict[Answers.DosAnswerId]).UtcDateTime,TimeSpan.FromMinutes(2));
        results.IsBillable.Should().Be(determination != "U");
    }
    
    public static IEnumerable<object[]> GetNormalityTestData
    {
        get{
            const string filePath = "../../../../Signify.PAD.Svc.System.Tests.Core/Data/normality.json";

            var jArray = JArray.Parse(File.ReadAllText(filePath));

            foreach (var normalityData in jArray)
            {
                yield return [(string)normalityData["leftValue"], 
                    (string)normalityData["rightValue"], 
                    (string)normalityData["leftNormality"], 
                    (string)normalityData["rightNormality"], 
                    (string)normalityData["determination"], 
                    (string)normalityData["lSeverity"], 
                    (string)normalityData["rSeverity"]];
            }
        }
    }
}