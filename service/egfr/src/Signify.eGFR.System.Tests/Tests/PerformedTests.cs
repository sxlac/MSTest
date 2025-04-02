using Signify.EvaluationsApi.Core.Values;
using Signify.eGFR.System.Tests.Core.Actions;
using Signify.eGFR.Core.Events.Akka;
using Signify.eGFR.System.Tests.Core.Constants;
using Signify.QE.MSTest.Attributes;
using Signify.eGFR.System.Tests.Core.Models.Kafka;

namespace Signify.eGFR.System.Tests.Tests;

[TestClass, TestCategory("regression")]
public class PerformedTests : PerformedActions
{
    public TestContext TestContext { get; set; }

    [RetryableTestMethod]
    public async Task ANC_T410_Performed_KED()
    {
        // Arrange
        var (member, appointment, evaluation) =
            await CoreApiActions.PrepareEvaluation();
        var answersDict = GenerateKedPerformedAnswers();
        // Act
        CoreApiActions.SetEvaluationAnswers(evaluation.EvaluationId,
            CoreApiActions.GetEvaluationAnswerList(answersDict));
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Stop);
        CoreApiActions.SetEvaluationStatus(evaluation.EvaluationId, EvaluationStatusCode.Finalized);

        // Assert
        // Database eGFR table
        var egfr = await GetExamByEvaluationId(evaluation.EvaluationId);
        egfr.EvaluationId.Should().Be(Convert.ToInt32(evaluation.EvaluationId));
        egfr.MemberPlanId.Should().Be(Convert.ToInt32(member.MemberPlanId));
        egfr.MemberId.Should().Be(Convert.ToInt32(member.MemberId));
        egfr.CenseoId.Should().Be(member.CenseoId);
        egfr.AppointmentId.Should().Be(appointment.AppointmentId);
        egfr.ProviderId.Should().Be(Provider.ProviderId);
        egfr.DateOfService.Should().Be(DateTime.Parse(answersDict[Answers.DosAnswerId]).Date);
        egfr.ClientId.Should().Be(member.ClientID);
        egfr.City.Should().Be(member.City);
        egfr.State.Should().Be(member.State);
        egfr.AddressLineOne.Should().Be(member.AddressLineOne);
        egfr.AddressLineTwo.Should().Be(member.AddressLineTwo);
        egfr.ZipCode.Should().Be(member.ZipCode);
        egfr.NationalProviderIdentifier.Should().Be(Provider.NationalProviderIdentifier);
        egfr.FirstName.Should().Be(member.FirstName);
        egfr.LastName.Should().Be(member.LastName);
        egfr.MiddleName.Should().Be(member.MiddleName);
        egfr.DateOfBirth.Should().Be(member.DateOfBirth);
        egfr.EvaluationReceivedDateTime.Should().BeCloseTo(evaluation.ReceivedDateTime, TimeSpan.FromSeconds(10));

        // Database status updates
        var expectedIds = new List<int>
        {
            ExamStatusCodes.ExamPerformed.ExamStatusCodeId
        };

        var finalizedEvent =
            await CoreKafkaActions.GetEvaluationEvent<EvaluationFinalizedEvent>(evaluation.EvaluationId,
                "EvaluationFinalizedEvent");
        await ValidateExamStatusCodesByEvaluationId(evaluation.EvaluationId, expectedIds);

        // Kafka performed status event
        var performed = await CoreKafkaActions.GetEgfrPerformedStatusEvent<PerformedEvent>(evaluation.EvaluationId);
        performed.Barcode.Should()
            .Be(answersDict[Answers.KedNumcodeAnswerId] + "|" + answersDict[Answers.KedAlphacodeAnswerId]);
        performed.ProductCode.Should().Be(TestConstants.Product);
        performed.EvaluationId.Should().Be(evaluation.EvaluationId);
        performed.MemberPlanId.Should().Be(member.MemberPlanId);
        performed.ProviderId.Should().Be(Provider.ProviderId);
        performed.CreatedDate.Should().BeCloseTo(finalizedEvent.CreatedDateTime, TimeSpan.FromSeconds(10));
        performed.ReceivedDate.Should().BeCloseTo(finalizedEvent.ReceivedDateTime, TimeSpan.FromSeconds(10));
    }
}