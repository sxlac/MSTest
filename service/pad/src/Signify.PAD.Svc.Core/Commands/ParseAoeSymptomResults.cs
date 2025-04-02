using MediatR;
using Microsoft.Extensions.Logging;
using Signify.PAD.Svc.Core.ApiClient.Response;
using Signify.PAD.Svc.Core.Constants.Questions.Aoe;
using Signify.PAD.Svc.Core.Exceptions;
using Signify.PAD.Svc.Core.Models;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.PAD.Svc.Core.Commands;

[ExcludeFromCodeCoverage]
public class ParseAoeSymptomResults(long evaluationId, IReadOnlyDictionary<int, ICollection<EvaluationAnswerModel>> answers, EvaluationAnswers evaluationAnswers) : ParseResultsBase(evaluationId, answers), IRequest<AoeSymptomAnswers>
{
    public EvaluationAnswers EvaluationAnswers { get; set; } = evaluationAnswers;
}

public class ParseAoeSymptomResultsHandler(ILogger<ParseAoeSymptomResultsHandler> logger) : ParseResultsHandlerBase(logger), IRequestHandler<ParseAoeSymptomResults, AoeSymptomAnswers>
{
    private const string Moderate = "Moderate";
    private const string Severe = "Severe";

    public Task<AoeSymptomAnswers> Handle(ParseAoeSymptomResults request, CancellationToken cancellationToken)
    {
        if (!TryParseBool(request, LegPainResolvedByOtcMedicationQuestion.QuestionId, LegPainResolvedByOtcMedicationQuestion.YesAnswerId, LegPainResolvedByOtcMedicationQuestion.NoAnswerId, out var footPainDisappearsOtc))
        {
            // ANC-6519 - We will return a null response here if we don't get a valid answer on the boolean question.  At the time of this ticket we are under the assumption that either all AoE Symptom questions
            // are answered or none of them are answered.  If we don't get an answer on this question we can be sure that none of the AoE questions were asked / answered and we do not have any
            // answers to store in the database.  By returning null we will ensure that we don't save the AoE Symptom results to the database.
            return Task.FromResult<AoeSymptomAnswers>(null);
        }

        TryParseBool(request, LegPainResolvedByMovementQuestion.QuestionId, LegPainResolvedByMovementQuestion.YesAnswerId, LegPainResolvedByMovementQuestion.NoAnswerId, out var footPainDisappearsWalkingOrDangling);

        var aoeSymptomAnswers = new AoeSymptomAnswers
        {
            LateralityCodeId = GetLateralityCode(request).Value,
            PedalPulseCodeId = GetPedalPulseCode(request).Value,
            FootPainDisappearsOtc = footPainDisappearsOtc,
            FootPainDisappearsWalkingOrDangling = footPainDisappearsWalkingOrDangling,
            AoeWithRestingLegPainConfirmed = HasAoeWithRestingLegPainConfirmed(request),
            ReasonAoeWithRestingLegPainNotConfirmed = GetReasonAoEWithRestingLegPainNotConfirmed(request)
        };

        aoeSymptomAnswers.HasSymptomsForAoeWithRestingLegPain = HasSymptomsForAoeWithRestingLegPain(aoeSymptomAnswers);
        aoeSymptomAnswers.HasClinicalSupportForAoeWithRestingLegPain = HasClinicalSupportForAoeWithRestingLegPain(request.EvaluationAnswers, aoeSymptomAnswers);

        return Task.FromResult(aoeSymptomAnswers);
    }

    private int? GetLateralityCode(ParseResultsBase request)
    {
        if (!TryGetExpected(request, LegPainQuestion.QuestionId, nameof(LegPainQuestion), out var lateralityQuestionAnswer))
        {
            return null;
        }

        return lateralityQuestionAnswer.AnswerId switch
        {
            LegPainQuestion.YesRightLegAnswerId => (int)LateralityCodes.Right,
            LegPainQuestion.YesLeftLegAnswerId => (int)LateralityCodes.Left,
            LegPainQuestion.YesBothLegsAnswerId => (int)LateralityCodes.Both,
            LegPainQuestion.NeitherAnswerId => (int?)(int)LateralityCodes.Neither,
            _ => throw new UnsupportedAnswerForQuestionException(request.EvaluationId, LegPainQuestion.QuestionId, lateralityQuestionAnswer.AnswerId, lateralityQuestionAnswer.AnswerValue),
        };
    }

    private int? GetPedalPulseCode(ParseResultsBase request)
    {
        if (!TryGetExpected(request, PedalPulsesQuestion.QuestionId, nameof(PedalPulsesQuestion), out var pedalPulseQuestionAnswer))
        {
            return null;
        }

        return pedalPulseQuestionAnswer.AnswerId switch
        {
            PedalPulsesQuestion.NormalAnswerId => (int)PedalPulseCodes.Normal,
            PedalPulsesQuestion.AbnormalLeftAnswerId => (int)PedalPulseCodes.AbnormalLeft,
            PedalPulsesQuestion.AbnormalRightAnswerId => (int)PedalPulseCodes.AbnormalRight,
            PedalPulsesQuestion.AbronmalBilateralAnswerId => (int)PedalPulseCodes.AbnormalBilateral,
            PedalPulsesQuestion.NotPerformedAnswerId => (int?)(int)PedalPulseCodes.NotPerformed,
            _ => throw new UnsupportedAnswerForQuestionException(request.EvaluationId, PedalPulsesQuestion.QuestionId, pedalPulseQuestionAnswer.AnswerId, pedalPulseQuestionAnswer.AnswerValue),
        };
    }

    private static bool TryParseBool(ParseResultsBase request, int questionId, int yesAnswerId, int noAnswerId, out bool result)
    {
        if (!TryGetOptional(request, questionId, out EvaluationAnswerModel answer))
        {
            result = default;
            return false;
        }

        if (answer.AnswerId == yesAnswerId)
        {
            result = true;
            return true;
        }

        if (answer.AnswerId == noAnswerId)
        {
            result = false;
            return true;
        }

        throw new UnsupportedAnswerForQuestionException(questionId, yesAnswerId, noAnswerId, answer.AnswerValue);
    }

    private static bool HasAoeWithRestingLegPainConfirmed(ParseResultsBase request)
    {
        if (!TryParseBool(request, AoeDiagnosisConfirmedQuestion.QuestionId, AoeDiagnosisConfirmedQuestion.ConfirmedAnswerId, AoeDiagnosisConfirmedQuestion.NotConfirmedAnswerId, out var hasAoeWithRestingLegPainConfirmed))
        {
            // If we don't have an answer for the question then we can determine that the member was not determined with resting leg pain
            return false;
        }

        return hasAoeWithRestingLegPainConfirmed;
    }

    private static bool HasSymptomsForAoeWithRestingLegPain(AoeSymptomAnswers aoeSymptomAnswers)
    {
        if (HasLateralityCodeThatMatchesForAoeWithRestingLegPain(aoeSymptomAnswers) &&
            aoeSymptomAnswers.FootPainDisappearsWalkingOrDangling &&
            !aoeSymptomAnswers.FootPainDisappearsOtc &&
            HasPedalPulseCodeThatMatchesForAoeWithRestingLegPain(aoeSymptomAnswers))
        {
            // ANC-3713 - Logic used to determine the Has Symptoms for AoE with Resting Leg Pain value:
            //  * The answer to the question on if there is pain in legs while resting with their feet elevated must be either Left, Right, or Both feet
            //  * The answer to the question on does the pain go away after walking or dangling the leg must be Yes
            //  * The answer to the question on does the pain go away after taking over the counter medication must be No
            //  * The answer to the question on Pedal Pulse must be either Abnormal-Left, Abnormal-Right, or Abnormal-Bilateral
            return true;
        }

        return false;
    }

    private static bool HasLateralityCodeThatMatchesForAoeWithRestingLegPain(AoeSymptomAnswers aoeSymptomAnswers)
    {
        // ANC-3713 - We can determine the laterality code has a match for AoE wtih Resting leg Pain while the laterality code is set to either Left, Right or Both
        if (aoeSymptomAnswers.LateralityCodeId == (int)LateralityCodes.Left ||
            aoeSymptomAnswers.LateralityCodeId == (int)LateralityCodes.Right ||
            aoeSymptomAnswers.LateralityCodeId == (int)LateralityCodes.Both)
        {
            return true;
        }

        return false;
    }

    private static bool HasPedalPulseCodeThatMatchesForAoeWithRestingLegPain(AoeSymptomAnswers aoeSymptomAnswers)
    {
        // ANC-3713 - We can determine the pedal pulse code has a match for AoE wtih Resting leg Pain while the pedal pulse code
        // is set to either Abnormal-Left, Abnormal-Right, or Abnormal-Bilateral

        if (aoeSymptomAnswers.PedalPulseCodeId == (int)PedalPulseCodes.AbnormalLeft ||
            aoeSymptomAnswers.PedalPulseCodeId == (int)PedalPulseCodes.AbnormalRight ||
            aoeSymptomAnswers.PedalPulseCodeId == (int)PedalPulseCodes.AbnormalBilateral)
        {
            return true;
        }

        return false;
    }

    private static bool HasClinicalSupportForAoeWithRestingLegPain(EvaluationAnswers evaluationAnswers, AoeSymptomAnswers aoeSymptomAnswers)
    {
        if (!aoeSymptomAnswers.HasSymptomsForAoeWithRestingLegPain)
        {
            // When the member does not have symptoms for AoE with resting leg pain we can determine that they do not have clinical support for resting leg pain
            return false;
        }

        var leftSeverity = evaluationAnswers.LeftSeverity ?? string.Empty;
        var rightSeverity = evaluationAnswers.RightSeverity ?? string.Empty;

        if (leftSeverity.Equals(Moderate) || leftSeverity.Equals(Severe) ||
            rightSeverity.Equals(Moderate) || rightSeverity.Equals(Severe))
        {
            // If the member has moderate or severe test results for either leg then we can determine that they have clinical support for resting leg pain
            return true;
        }

        return false;
    }

    private static string GetReasonAoEWithRestingLegPainNotConfirmed(ParseResultsBase request)
    {
        if (!HasAoeWithRestingLegPainConfirmed(request) &&
            TryGetOptional(request, ReasonAoeWithRestingLegPainNotConfirmedQuestion.QuestionId, out var answer))
        {
            return answer.AnswerValue;
        }
        return string.Empty;
    }
}