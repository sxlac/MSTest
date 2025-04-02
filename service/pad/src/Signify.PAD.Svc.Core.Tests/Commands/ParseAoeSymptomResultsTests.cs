using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.PAD.Svc.Core.ApiClient.Response;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Constants.Questions.Aoe;
using Signify.PAD.Svc.Core.Constants.Questions.NotPerformed;
using Signify.PAD.Svc.Core.Exceptions;
using Signify.PAD.Svc.Core.Models;
using Signify.PAD.Svc.Core.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Commands;

public class ParseAoeSymptomResultsTests
{
    private static ParseAoeSymptomResultsHandler CreateSubject()
        => new(A.Dummy<ILogger<ParseAoeSymptomResultsHandler>>());

    private static ParseAoeSymptomResults CreateParseAoeSymptomResults(IEnumerable<EvaluationAnswerModel> answers, EvaluationAnswers evaluationAnswers)
        => new(1, new AnswerLookupBuilderService().BuildLookup(answers), evaluationAnswers);

    [Theory]
    [InlineData(LegPainQuestion.YesRightLegAnswerId, (int)LateralityCodes.Right)]
    [InlineData(LegPainQuestion.YesLeftLegAnswerId, (int)LateralityCodes.Left)]
    [InlineData(LegPainQuestion.YesBothLegsAnswerId, (int)LateralityCodes.Both)]
    [InlineData(LegPainQuestion.NeitherAnswerId, (int)LateralityCodes.Neither)]
    public async Task Handle_EvaluationAoeAnswersForLaterality_ReturnCorrectlyLateralityCode(int lateralityAnswerId, int expectedLateralityCode)
    {
        // Arrange
        var subject = CreateSubject();

        var request = CreateRequest
        (
            lateralityAnswerId,
            LegPainResolvedByOtcMedicationQuestion.YesAnswerId,
            LegPainResolvedByMovementQuestion.YesAnswerId,
            PedalPulsesQuestion.NormalAnswerId,
            AoeDiagnosisConfirmedQuestion.ConfirmedAnswerId,
            new EvaluationAnswers()
        );

        // Act
        var result = await subject.Handle(request, default);

        // Assert
        Assert.Equal(expectedLateralityCode, result.LateralityCodeId);
    }

    [Theory]
    [InlineData(PedalPulsesQuestion.NormalAnswerId, (int)PedalPulseCodes.Normal)]
    [InlineData(PedalPulsesQuestion.AbnormalLeftAnswerId, (int)PedalPulseCodes.AbnormalLeft)]
    [InlineData(PedalPulsesQuestion.AbnormalRightAnswerId, (int)PedalPulseCodes.AbnormalRight)]
    [InlineData(PedalPulsesQuestion.AbronmalBilateralAnswerId, (int)PedalPulseCodes.AbnormalBilateral)]
    [InlineData(PedalPulsesQuestion.NotPerformedAnswerId, (int)PedalPulseCodes.NotPerformed)]
    public async Task Handle_EvaluationAoeAnswersForPedalPulse_ReturnCorrectlyPedalPulseCode(int pedalPulseAnswerId, int expectedPedalPulseCode)
    {
        // Arrange
        var subject = CreateSubject();

        var request = CreateRequest
        (
            LegPainQuestion.YesBothLegsAnswerId,
            LegPainResolvedByOtcMedicationQuestion.YesAnswerId,
            LegPainResolvedByMovementQuestion.YesAnswerId,
            pedalPulseAnswerId,
            AoeDiagnosisConfirmedQuestion.ConfirmedAnswerId,
            new EvaluationAnswers()
        );

        // Act
        var result = await subject.Handle(request, default);

        // Assert
        Assert.Equal(expectedPedalPulseCode, result.PedalPulseCodeId);
    }

    [Theory]
    [InlineData(LegPainResolvedByOtcMedicationQuestion.YesAnswerId, true)]
    [InlineData(LegPainResolvedByOtcMedicationQuestion.NoAnswerId, false)]
    public async Task Handle_EvaluationAoeAnswersForFootPainResolvedByMedication_ReturnCorrectResponse(int answerId, bool expectedResponse)
    {
        // Arrange
        var subject = CreateSubject();

        var request = CreateRequest
        (
            LegPainQuestion.YesBothLegsAnswerId,
            answerId,
            LegPainResolvedByMovementQuestion.YesAnswerId,
            PedalPulsesQuestion.NormalAnswerId,
            AoeDiagnosisConfirmedQuestion.ConfirmedAnswerId,
            new EvaluationAnswers()
        );

        // Act
        var result = await subject.Handle(request, default);

        // Assert
        Assert.Equal(expectedResponse, result.FootPainDisappearsOtc);
    }

    [Theory]
    [InlineData(LegPainResolvedByMovementQuestion.YesAnswerId, true)]
    [InlineData(LegPainResolvedByMovementQuestion.NoAnswerId, false)]
    public async Task Handle_EvaluationAoeAnswersForFootPainResolvedByMovement_ReturnCorrectResponse(int answerId, bool expectedResponse)
    {
        // Arrange
        var subject = CreateSubject();

        var request = CreateRequest
        (
            LegPainQuestion.YesBothLegsAnswerId,
            LegPainResolvedByOtcMedicationQuestion.YesAnswerId,
            answerId,
            PedalPulsesQuestion.NormalAnswerId,
            AoeDiagnosisConfirmedQuestion.ConfirmedAnswerId,
            new EvaluationAnswers()
        );

        // Act
        var result = await subject.Handle(request, default);

        // Assert
        Assert.Equal(expectedResponse, result.FootPainDisappearsWalkingOrDangling);
    }

    [Fact]
    public async Task Handle_InvalidLateralityCodeAnswer_ThrowsUnsupportedAnswerException()
    {
        // Arrange
        var subject = CreateSubject();

        var request = CreateRequest
        (
            1,
            LegPainResolvedByOtcMedicationQuestion.YesAnswerId,
            LegPainResolvedByMovementQuestion.YesAnswerId,
            PedalPulsesQuestion.NormalAnswerId,
            AoeDiagnosisConfirmedQuestion.ConfirmedAnswerId,
            new EvaluationAnswers()
        );

        // Act
        // Assert
        await Assert.ThrowsAnyAsync<UnsupportedAnswerForQuestionException>(async () =>
            await subject.Handle(request, default));
    }

    [Fact]
    public async Task Handle_InvalidPedalPulseCodeAnswer_ThrowsUnsupportedAnswerException()
    {
        // Arrange
        var subject = CreateSubject();

        var request = CreateRequest
        (
            LegPainQuestion.YesBothLegsAnswerId,
            LegPainResolvedByOtcMedicationQuestion.YesAnswerId,
            LegPainResolvedByMovementQuestion.YesAnswerId,
            1,
            AoeDiagnosisConfirmedQuestion.ConfirmedAnswerId,
            new EvaluationAnswers()
        );

        // Act
        // Assert
        await Assert.ThrowsAnyAsync<UnsupportedAnswerForQuestionException>(async () =>
            await subject.Handle(request, default));
    }

    [Fact]
    public async Task Handle_InvalidBooleanAnswer_ThrowsUnsupportedAnswerException()
    {
        // Arrange
        var subject = CreateSubject();

        var request = CreateRequest
        (
            LegPainQuestion.YesBothLegsAnswerId,
            LegPainResolvedByOtcMedicationQuestion.YesAnswerId,
            1,
            PedalPulsesQuestion.NormalAnswerId,
            AoeDiagnosisConfirmedQuestion.ConfirmedAnswerId,
            new EvaluationAnswers()
        );

        // Act
        // Assert
        await Assert.ThrowsAnyAsync<UnsupportedAnswerForQuestionException>(async () =>
            await subject.Handle(request, default));
    }

    [Fact]
    public async Task Handle_UnansweredHasLegPainWhileRestingQuestion_SetAoeWithRestingLegPainConfirmedToFalse()
    {
        // Arrange
        var subject = CreateSubject();

        var answers = new List<EvaluationAnswerModel>
        {
            new()
            {
                QuestionId = LegPainQuestion.QuestionId,
                AnswerId = LegPainQuestion.NeitherAnswerId
            },
            new()
            {
                QuestionId = LegPainResolvedByOtcMedicationQuestion.QuestionId,
                AnswerId = LegPainResolvedByOtcMedicationQuestion.NoAnswerId
            },
            new()
            {
                QuestionId = LegPainResolvedByMovementQuestion.QuestionId,
                AnswerId = LegPainResolvedByMovementQuestion.NoAnswerId
            },
            new()
            {
                QuestionId = PedalPulsesQuestion.QuestionId,
                AnswerId = PedalPulsesQuestion.NotPerformedAnswerId
            }
        };

        var request = CreateParseAoeSymptomResults(answers, new EvaluationAnswers());

        // Act
        var result = await subject.Handle(request, default);

        // Assert
        Assert.False(result.AoeWithRestingLegPainConfirmed);
    }

    [Fact]
    public async Task Handle_NoAoeAnswers_ReturnNull()
    {
        // Arrange
        var subject = CreateSubject();

        var answers = new List<EvaluationAnswerModel>
        {
            new()
            {
                QuestionId = ReasonNotPerformedQuestion.QuestionId,
                AnswerId = ReasonNotPerformedQuestion.MemberRefusedAnswerId
            }
        };
        var request = CreateParseAoeSymptomResults(answers, new EvaluationAnswers());

        // Act
        var result = await subject.Handle(request, default);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(AoeDiagnosisConfirmedQuestion.ConfirmedAnswerId, true)]
    [InlineData(AoeDiagnosisConfirmedQuestion.NotConfirmedAnswerId, false)]
    public async Task Handle_EvaluateAnswersForAoeWithRestingLegPainConfirmed_ReturnsCorrectValue(int diagnosisAnswerId, bool expectedResponse)
    {
        // Arrange
        var subject = CreateSubject();

        var request = CreateRequest(LegPainQuestion.YesBothLegsAnswerId, LegPainResolvedByOtcMedicationQuestion.NoAnswerId, LegPainResolvedByMovementQuestion.YesAnswerId, PedalPulsesQuestion.AbnormalLeftAnswerId, diagnosisAnswerId, new EvaluationAnswers());

        // Act
        var result = await subject.Handle(request, default);

        // Assert
        Assert.Equal(expectedResponse, result.AoeWithRestingLegPainConfirmed);
    }

    [Theory]
    [InlineData("Severe", true)]
    [InlineData("Normal", false)]
    public async Task Handle_MemberHasLegSeverityScoreAndRestingLegPainConfirmed_SetClinicalSupportValue(string severity, bool expectedResult)
    {
        // Arrange
        var subject = CreateSubject();

        var evaluationAnswers = new EvaluationAnswers
        {
            LeftSeverity = severity,
            RightSeverity = severity
        };

        var request = CreateRequest(LegPainQuestion.YesBothLegsAnswerId, LegPainResolvedByOtcMedicationQuestion.NoAnswerId, LegPainResolvedByMovementQuestion.YesAnswerId, PedalPulsesQuestion.AbnormalLeftAnswerId, AoeDiagnosisConfirmedQuestion.ConfirmedAnswerId, evaluationAnswers);

        // Act
        var result = await subject.Handle(request, default);

        // Assert
        Assert.Equal(expectedResult, result.HasClinicalSupportForAoeWithRestingLegPain);
    }

    [Fact]
    public async Task Handle_MemberDoesNotHaveSymptomsForAoeWithRestingLegPain_SetClinicalSupportToFalse()
    {
        // Arrange
        var subject = CreateSubject();

        var evaluationAnswers = new EvaluationAnswers
        {
            LeftSeverity = "Severe",
            RightSeverity = "Severe"
        };

        var request = CreateRequest(LegPainQuestion.YesBothLegsAnswerId, LegPainResolvedByOtcMedicationQuestion.YesAnswerId, LegPainResolvedByMovementQuestion.YesAnswerId, PedalPulsesQuestion.AbnormalLeftAnswerId, AoeDiagnosisConfirmedQuestion.NotConfirmedAnswerId, evaluationAnswers);

        // Act
        var result = await subject.Handle(request, default);

        // Assert
        Assert.False(result.HasClinicalSupportForAoeWithRestingLegPain);
    }

    [Fact]
    public async Task Handle_MemberDoesNotHaveRestingLegPainConfirmed_NotesAreRead()
    {
        // Arrange
        var subject = CreateSubject();

        var evaluationAnswers = new EvaluationAnswers
        {
            LeftSeverity = "Severe",
            RightSeverity = "Severe",
        };

        var request = CreateRequest(LegPainQuestion.YesBothLegsAnswerId, LegPainResolvedByOtcMedicationQuestion.NoAnswerId, LegPainResolvedByMovementQuestion.YesAnswerId, PedalPulsesQuestion.AbnormalLeftAnswerId, AoeDiagnosisConfirmedQuestion.NotConfirmedAnswerId, evaluationAnswers);

        // Act
        var result = await subject.Handle(request, default);

        // Assert
        Assert.Equal("Leg was unavailable", result.ReasonAoeWithRestingLegPainNotConfirmed);
    }

    [Fact]
    public async Task Handle_MemberDoesHaveRestingLegPainConfirmed_ReasonNotCollectedNotesAreNotRead()
    {
        // Arrange
        var subject = CreateSubject();

        var evaluationAnswers = new EvaluationAnswers
        {
            LeftSeverity = "Severe",
            RightSeverity = "Severe",
        };

        var request = CreateRequest(LegPainQuestion.YesBothLegsAnswerId, LegPainResolvedByOtcMedicationQuestion.NoAnswerId, LegPainResolvedByMovementQuestion.YesAnswerId, PedalPulsesQuestion.AbnormalLeftAnswerId, AoeDiagnosisConfirmedQuestion.ConfirmedAnswerId, evaluationAnswers);

        // Act
        var result = await subject.Handle(request, default);

        // Assert
        Assert.Equal(string.Empty, result.ReasonAoeWithRestingLegPainNotConfirmed);
    }

    [Fact]
    public async Task Handle_EvaluateMemberThatHasAoeSymptomAnswers_SetHasSymptomsForAoeWithRestingLegPainToTrue()
    {
        // Arrange
        var subject = CreateSubject();

        var evaluationAnswers = new EvaluationAnswers();

        var request = CreateRequest(LegPainQuestion.YesBothLegsAnswerId, LegPainResolvedByOtcMedicationQuestion.NoAnswerId, LegPainResolvedByMovementQuestion.YesAnswerId, PedalPulsesQuestion.AbnormalLeftAnswerId, AoeDiagnosisConfirmedQuestion.ConfirmedAnswerId, evaluationAnswers);

        // Act
        var result = await subject.Handle(request, default);

        // Assert
        Assert.True(result.HasSymptomsForAoeWithRestingLegPain);
    }

    [Fact]
    public async Task Handle_EvaluateMemberThatDoesntHaveAoeSymptomAnswers_SetHasSymptomsForAoeWithRestingLegPainToFalse()
    {
        // Arrange
        var subject = CreateSubject();

        var evaluationAnswers = new EvaluationAnswers();

        var request = CreateRequest(LegPainQuestion.YesBothLegsAnswerId, LegPainResolvedByOtcMedicationQuestion.YesAnswerId, LegPainResolvedByMovementQuestion.YesAnswerId, PedalPulsesQuestion.AbnormalLeftAnswerId, AoeDiagnosisConfirmedQuestion.ConfirmedAnswerId, evaluationAnswers);

        // Act
        var result = await subject.Handle(request, default);

        // Assert
        Assert.False(result.HasSymptomsForAoeWithRestingLegPain);
    }

    [Theory]
    [InlineData(52178, true)]
    [InlineData(52179, true)]
    [InlineData(52180, true)]
    [InlineData(52181, false)]
    public async Task Handle_EvaluateDifferentLateralityCodes_SetHasSymptomsForAoeWithRestingLegPainCorrectly(int lateralityCodeId, bool expectedResult)
    {
        // Arrange
        var subject = CreateSubject();

        var evaluationAnswers = new EvaluationAnswers();

        var request = CreateRequest(lateralityCodeId, LegPainResolvedByOtcMedicationQuestion.NoAnswerId, LegPainResolvedByMovementQuestion.YesAnswerId, PedalPulsesQuestion.AbnormalLeftAnswerId, AoeDiagnosisConfirmedQuestion.ConfirmedAnswerId, evaluationAnswers);

        // Act
        var result = await subject.Handle(request, default);

        // Assert
        Assert.Equal(expectedResult, result.HasSymptomsForAoeWithRestingLegPain);
    }

    [Theory]
    [InlineData(52186, false)]
    [InlineData(52187, true)]
    [InlineData(52188, true)]
    [InlineData(52189, true)]
    [InlineData(52190, false)]
    public async Task Handle_EvaluateDifferentPedalPulseCodes_SetHasSymptomsForAoeWithRestingLegPainCorrectly(int pedalPulseCodeId, bool expectedResult)
    {
        // Arrange
        var subject = CreateSubject();

        var evaluationAnswers = new EvaluationAnswers();

        var request = CreateRequest(LegPainQuestion.YesBothLegsAnswerId, LegPainResolvedByOtcMedicationQuestion.NoAnswerId, LegPainResolvedByMovementQuestion.YesAnswerId, pedalPulseCodeId, AoeDiagnosisConfirmedQuestion.ConfirmedAnswerId, evaluationAnswers);

        // Act
        var result = await subject.Handle(request, default);

        // Assert
        Assert.Equal(expectedResult, result.HasSymptomsForAoeWithRestingLegPain);
    }

    private static ParseAoeSymptomResults CreateRequest(int lateraltiyAnswerId, int medicationAnswerId, int movementAnswerId, int pedalPulseAnswerId, int diagnosisConfirmedAnswerId, EvaluationAnswers evaluationAnswers)
    {
        return CreateParseAoeSymptomResults(
        [
            new EvaluationAnswerModel
            {
                QuestionId = LegPainQuestion.QuestionId,
                AnswerId = lateraltiyAnswerId
            },
            new EvaluationAnswerModel
            {
                QuestionId = LegPainResolvedByOtcMedicationQuestion.QuestionId,
                AnswerId = medicationAnswerId
            },
            new EvaluationAnswerModel
            {
                QuestionId = LegPainResolvedByMovementQuestion.QuestionId,
                AnswerId = movementAnswerId
            },
            new EvaluationAnswerModel
            {
                QuestionId = PedalPulsesQuestion.QuestionId,
                AnswerId = pedalPulseAnswerId
            },
            new EvaluationAnswerModel
            {
                QuestionId = AoeDiagnosisConfirmedQuestion.QuestionId,
                AnswerId = diagnosisConfirmedAnswerId
            },
            new EvaluationAnswerModel
            {
                QuestionId = ReasonAoeWithRestingLegPainNotConfirmedQuestion.QuestionId,
                AnswerId = ReasonAoeWithRestingLegPainNotConfirmedQuestion.AnswerId,
                AnswerValue = "Leg was unavailable"
            }
        ], evaluationAnswers);
    }
}
