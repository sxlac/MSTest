using MediatR;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.ApiClients.EvaluationApi.Responses;
using Signify.Spirometry.Core.Constants.Questions.Performed;
using Signify.Spirometry.Core.Exceptions;
using Signify.Spirometry.Core.Factories;
using Signify.Spirometry.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FT = Signify.Spirometry.Core.Factories.IOccurrenceFrequencyConverterFactory.FrequencyConverterType;
using TCT = Signify.Spirometry.Core.Factories.ITrileanTypeConverterFactory.TrileanConverterType;

namespace Signify.Spirometry.Core.Commands
{
    /// <summary>
    /// Command to parse evaluation answers where a Spirometry exam was performed
    /// </summary>
    public class ParsePerformedResults : ParseResultsBase, IRequest<PerformedExamModel>
    {
        public ParsePerformedResults(int evaluationId, IReadOnlyDictionary<int, ICollection<EvaluationAnswerModel>> answers)
            : base(evaluationId, answers)
        {
        }
    }

    public class ParsePerformedResultsHandler : ParseResultsHandlerBase,
        IRequestHandler<ParsePerformedResults, PerformedExamModel>
    {
        private readonly IOccurrenceFrequencyConverterFactory _frequencyConverterFactory;

        public ParsePerformedResultsHandler(ILogger<ParsePerformedResultsHandler> logger,
            ITrileanTypeConverterFactory trileanTypeConverterFactory,
            IOccurrenceFrequencyConverterFactory frequencyConverterFactory)
            : base(logger, trileanTypeConverterFactory)
        {
            _frequencyConverterFactory = frequencyConverterFactory;
        }

        public Task<PerformedExamModel> Handle(ParsePerformedResults request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PerformedExamModel(request.EvaluationId, ParseResults(request)));
        }

        private RawExamResult ParseResults(ParseResultsBase request)
        {
            var results = new RawExamResult
            {
                EvaluationId = request.EvaluationId,
                SessionGrade = GetSessionGrade(request)
            };

            if (TryGetExpected(request, FvcQuestion.QuestionId, "FVC", out var answerModel))
                results.Fvc = answerModel.AnswerValue;

            if (TryGetExpected(request, Fev1Question.QuestionId, "FEV1", out answerModel))
                results.Fev1 = answerModel.AnswerValue;

            if (TryGetExpected(request, Fev1OverFvcQuestion.QuestionId, "FEV1/FVC", out answerModel))
                results.Fev1FvcRatio = answerModel.AnswerValue;

            if (TryParseTrilean(request, HasHighSymptomsQuestion.QuestionId, TCT.HasHighSymptom, out var trileanType))
                results.HasHighSymptom = trileanType;

            if (TryParseTrilean(request, HasEnvOrExpRiskQuestion.QuestionId, TCT.HasEnvOrExpRisk, out trileanType))
                results.HasEnvOrExpRisk = trileanType;

            if (TryParseTrilean(request, HasHighComorbidityQuestion.QuestionId, TCT.HasHighComorbidity, out trileanType))
                results.HasHighComorbidity = trileanType;

            SetLungFunctionResults(request, results);

            SetCopdDiagnosis(request, results);

            SetPreviousDiagnoses(request, results);

            return results;
        }

        private static void SetCopdDiagnosis(ParseResultsBase request, RawExamResult results)
        {
            bool TrySet(int questionId, int answerId)
            {
                if (!TryGetOptional(request, questionId, out var copdAnswer))
                    return false;

                if (copdAnswer.AnswerId != answerId)
                    throw new UnsupportedAnswerForQuestionException(questionId, copdAnswer.AnswerId, copdAnswer.AnswerValue);

                results.CopdDiagnosis = true;
                return true;
            }

            // Set CopdDiagnosis on the results to `true` if either question has it answered as `true`
            _ = TrySet(CopdDiagnosisQuestion.Assessment.QuestionId, CopdDiagnosisQuestion.Assessment.YesAnswerId) ||
                TrySet(CopdDiagnosisQuestion.Heent.QuestionId, CopdDiagnosisQuestion.Heent.YesAnswerId);
        }

        private void SetLungFunctionResults(ParseResultsBase request, RawExamResult results)
        {
            if (TryParseOptionalBool(request, HasSmokedTobaccoQuestion.QuestionId, HasSmokedTobaccoQuestion.YesAnswerId, HasSmokedTobaccoQuestion.NoAnswerId, out var result))
                results.HasSmokedTobacco = result;

            if (TryParseOptionalInt(request, TotalYearsSmokingQuestion.QuestionId, TotalYearsSmokingQuestion.AnswerId, out var intResult))
                results.TotalYearsSmoking = intResult;

            if (TryParseOptionalBool(request, ProducesSputumWithCoughQuestion.QuestionId, ProducesSputumWithCoughQuestion.YesAnswerId, ProducesSputumWithCoughQuestion.NoAnswerId, out result))
                results.ProducesSputumWithCough = result;

            if (TryParseFrequency(request, CoughMucusFrequencyQuestion.QuestionId, FT.CoughMucus, out var frequency))
                results.CoughMucusFrequency = frequency;

            if (TryParseTrilean(request, HadWheezingPast12moQuestion.QuestionId, TCT.HadWheezingPast12mo, out var trileanType))
                results.HadWheezingPast12mo = trileanType;

            if (TryParseTrilean(request, GetsShortnessOfBreathAtRestQuestion.QuestionId, TCT.GetsShortnessOfBreathAtRest, out trileanType))
                results.GetsShortnessOfBreathAtRest = trileanType;

            if (TryParseTrilean(request, GetsShortnessOfBreathWithMildExertionQuestion.QuestionId, TCT.GetsShortnessOfBreathWithMildExertion, out trileanType))
                results.GetsShortnessOfBreathWithMildExertion = trileanType;

            if (TryParseFrequency(request, NoisyChestFrequencyQuestion.QuestionId, FT.NoisyChest, out frequency))
                results.NoisyChestFrequency = frequency;

            if (TryParseFrequency(request, ShortnessOfBreathPhysicalActivityFrequencyQuestion.QuestionId, FT.ShortnessOfBreathPhysicalActivity, out frequency))
                results.ShortnessOfBreathPhysicalActivityFrequency = frequency;

            if (TryParseOptionalInt(request, LungFunctionQuestionnaireScoreQuestion.QuestionId, LungFunctionQuestionnaireScoreQuestion.AnswerId, out intResult))
                results.LungFunctionQuestionnaireScore = intResult;
        }

        private static void SetPreviousDiagnoses(ParseResultsBase request, RawExamResult results)
        {
            void AddDiagnosesIfFound(int questionId, int answerId)
            {
                if (!request.Answers.TryGetValue(questionId, out var answers))
                    return;

                foreach (var answer in answers)
                {
                    if (answer.AnswerId != answerId || string.IsNullOrEmpty(answer.AnswerValue))
                        continue;

                    results.PreviousDiagnoses.Add(answer.AnswerValue);
                }
            }

            AddDiagnosesIfFound(DiagnosisHistoryQuestion.QuestionId, DiagnosisHistoryQuestion.DiagnosesAnswerId);

            // The following two questions/answers are soon to be deprecated. Leaving in, though, so we can re-process
            // evaluations finalized prior to the consolidation into the above question/answer, and so we are forward-
            // compatible with their upcoming breaking change of removing these.
            AddDiagnosesIfFound(ChartReviewDiagnosesQuestion.QuestionId, ChartReviewDiagnosesQuestion.DiagnosesAnswerId);
            AddDiagnosesIfFound(DocumentedAndAdditionalDiagnosesQuestion.QuestionId, DocumentedAndAdditionalDiagnosesQuestion.DiagnosesAnswerId);
        }

        private SessionGrade? GetSessionGrade(ParseResultsBase request)
        {
            const int questionId = SessionGradeQuestion.QuestionId;

            if (!TryGetExpected(request, questionId, "SessionGrade", out var sessionGradeQ))
                return null;

            switch (sessionGradeQ.AnswerId)
            {
                case SessionGradeQuestion.AnswerId:
                    if (!Enum.TryParse(sessionGradeQ.AnswerValue, ignoreCase: true, out SessionGrade sessionGrade))
                        throw new UnsupportedAnswerForQuestionException(questionId, sessionGradeQ.AnswerId, sessionGradeQ.AnswerValue);
                    return sessionGrade;
                // The below AnswerId's are all deprecated, but keeping here in case there are providers running
                // on the old app version, and in case we ever need to reprocess an evaluation finalized before
                // the change to the newer AnswerId above.
                case SessionGradeQuestion.AAnswerId:
                    return SessionGrade.A;
                case SessionGradeQuestion.BAnswerId:
                    return SessionGrade.B;
                case SessionGradeQuestion.CAnswerId:
                    return SessionGrade.C;
                case SessionGradeQuestion.DAnswerId:
                    return SessionGrade.D;
                case SessionGradeQuestion.EAnswerId:
                    return SessionGrade.E;
                case SessionGradeQuestion.FAnswerId:
                    return SessionGrade.F;
                default:
                    throw new UnsupportedAnswerForQuestionException(questionId, sessionGradeQ.AnswerId, sessionGradeQ.AnswerValue);
            }
        }

        private static bool TryParseOptionalBool(ParseResultsBase request, int questionId,
            int yesAnswerId, int noAnswerId, out bool result)
        {
            if (!TryGetOptional(request, questionId, out var answerModel))
            {
                result = default;
                return false;
            }

            if (answerModel.AnswerId == yesAnswerId)
            {
                result = true;
                return true;
            }

            if (answerModel.AnswerId == noAnswerId)
            {
                result = false;
                return true;
            }

            throw new UnsupportedAnswerForQuestionException(questionId, answerModel.AnswerId, answerModel.AnswerValue);
        }

        private static bool TryParseOptionalInt(ParseResultsBase request, int questionId,
            int answerId, out int result)
        {
            if (!TryGetOptional(request, questionId, out var answerModel))
            {
                result = default;
                return false;
            }

            if (answerModel.AnswerId != answerId)
                throw new UnsupportedAnswerForQuestionException(questionId, answerModel.AnswerId, answerModel.AnswerValue);

            if (!int.TryParse(answerModel.AnswerValue, out result))
                throw new AnswerValueFormatException(questionId, answerModel.AnswerId, answerModel.AnswerValue);

            return true;
        }

        private bool TryParseFrequency(ParseResultsBase request, int questionId,
            FT converterType, out OccurrenceFrequency? frequency)
        {
            if (!TryGetOptional(request, questionId, out var answerModel))
            {
                frequency = null;
                return false;
            }

            var converter = _frequencyConverterFactory.Create(converterType);

            if (!converter.TryConvert(answerModel.AnswerId, out var ft))
                throw new UnsupportedAnswerForQuestionException(questionId, answerModel.AnswerId, answerModel.AnswerValue);

            frequency = ft;
            return true;
        }
    }
}
