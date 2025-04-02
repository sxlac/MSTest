using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.PAD.Svc.Core.ApiClient.Response;
using Signify.PAD.Svc.Core.Constants;
using Signify.PAD.Svc.Core.Constants.Questions.Performed;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.PAD.Svc.Core.Commands
{
    /// <summary>
    /// Command to parse evaluation answers where a PAD exam was performed
    /// </summary>
    public class ParsePerformedResults : ParseResultsBase, IRequest<EvaluationAnswers>
    {
        public ParsePerformedResults(long evaluationId, IReadOnlyDictionary<int, ICollection<EvaluationAnswerModel>> answers)
            : base(evaluationId, answers)
        {
        }
    }

    public class ParsePerformedResultsHandler : ParseResultsHandlerBase, IRequestHandler<ParsePerformedResults, EvaluationAnswers>
    {
        private readonly PADDataContext _context;

        public ParsePerformedResultsHandler(ILogger<ParsePerformedResultsHandler> logger, PADDataContext context)
            : base(logger)
        {
            _context = context;
        }

        public async Task<EvaluationAnswers> Handle(ParsePerformedResults request, CancellationToken cancellationToken)
        {
            var orderedSeverities = await _context.SeverityLookup
                .AsNoTracking()
                .OrderBy(each => each.MaxScore)
                .ToListAsync(cancellationToken);

            GetSideResultAnswers(request, out var left, out var right);

            var result = new EvaluationAnswers
            {
                IsPadPerformedToday = true
            };

            var validated = GetValidatedResults(request.EvaluationId, left, orderedSeverities, true);
            result.LeftScore = validated.Score;
            result.LeftSeverity = validated.Severity;
            result.LeftException = validated.Exception;
            result.LeftNormalityIndicator = validated.NormalityIndicator;
            result.LeftScoreAnswerValue = validated.ScoreAnswerValue;

            validated = GetValidatedResults(request.EvaluationId, right, orderedSeverities, false);
            result.RightScore = validated.Score;
            result.RightSeverity = validated.Severity;
            result.RightException = validated.Exception;
            result.RightNormalityIndicator = validated.NormalityIndicator;
            result.RightScoreAnswerValue = validated.ScoreAnswerValue;

            return result;
        }

        private static void GetSideResultAnswers(ParseResultsBase request,
            out EvaluationAnswerModel left, out EvaluationAnswerModel right)
        {
            TryGetOptional(request, PadTestingResultsLeftQuestion.QuestionId, out left);
            TryGetOptional(request, PadTestingResultsRightQuestion.QuestionId, out right);
        }

        private class ValidatedResult
        {
            public bool IsValid { get; private set; }
            public string ScoreAnswerValue { get; private set; }
            public string Score { get; private set; }
            public string Severity { get; private set; }
            public string NormalityIndicator { get; private set; }
            public string Exception { get; private set; }

            private ValidatedResult() { }

            public static ValidatedResult FromResult(string scoreAnswerValue, string score, SeverityLookup severityLookup)
            {
                return new ValidatedResult
                {
                    IsValid = true,
                    ScoreAnswerValue = scoreAnswerValue,
                    Score = score,
                    Severity = severityLookup.Severity,
                    NormalityIndicator = severityLookup.NormalityIndicator
                };
            }

            public static ValidatedResult FromException(string exception, string scoreAnswerValue = null)
            {
                return new ValidatedResult
                {
                    IsValid = false,
                    ScoreAnswerValue = scoreAnswerValue,
                    NormalityIndicator = Application.NormalityIndicator.Undetermined,
                    Exception = exception
                };
            }

            public static ValidatedResult FromExceptionWithScore(string exception, string score, string scoreAnswerValue = null)
            {
                return new ValidatedResult
                {
                    IsValid = false,
                    ScoreAnswerValue = scoreAnswerValue,
                    Score = score,
                    NormalityIndicator = Application.NormalityIndicator.Undetermined,
                    Exception = exception
                };
            }
        }

        private ValidatedResult GetValidatedResults(long evaluationId, EvaluationAnswerModel answer, IList<SeverityLookup> orderedSeverities, bool isLeft)
        {
            // "PAD Testing Results ([left/right])" question
            var questionId = isLeft ? PadTestingResultsLeftQuestion.QuestionId : PadTestingResultsRightQuestion.QuestionId;
            var side = isLeft ? "Left" : "Right";

            void LogWarn(string message)
            {
                Logger.LogWarning("PAD testing results ({Side}) question ({QuestionId}) {Message}, for EvaluationId={EvaluationId}",
                    side, questionId, message, evaluationId);
            }

            if (answer == null)
            {
                LogWarn("was not answered");

                return ValidatedResult.FromException(Application.ResultException.NotSupplied);
            }

            if (string.IsNullOrWhiteSpace(answer.AnswerValue))
            {
                LogWarn("answer is null or whitespace");

                return ValidatedResult.FromException(Application.ResultException.NotSupplied);
            }

            if (!decimal.TryParse(answer.AnswerValue, out var score))
            {
                LogWarn($"answer is not a decimal (\"{answer.AnswerValue}\"");

                return ValidatedResult.FromExceptionWithScore(Application.ResultException.Malformed, answer.AnswerValue, answer.AnswerValue);
            }

            var severity = GetSeverity(score, orderedSeverities);
            if (severity == null)
            {
                LogWarn($"answer value ({score}) is not within valid range");

                return ValidatedResult.FromExceptionWithScore(Application.ResultException.OutOfRange, score.ToString(), answer.AnswerValue);
            }

            return ValidatedResult.FromResult(answer.AnswerValue, score.ToString(), severity);
        }

        private static SeverityLookup GetSeverity(decimal score, IList<SeverityLookup> orderedSeverities)
        {
            if (score < orderedSeverities.First().MinScore || score > orderedSeverities.Last().MaxScore)
                return null;

            // Valid range is >= 0 and <= 1.4
            // As you can see from values in db below, the severity ranges aren't really setup correctly, as one has an
            // inclusive upper limit, while the rest are exclusive. The code below properly handles this in case the
            // form is ever updated to support more than a two-decimal precision, we wouldn't risk saying the value is
            // out of range (ex consider 0.895)

            // As of now, SeverityLookup in db:
            // 0.00 - 0.29
            // 0.30 - 0.59
            // 0.60 - 0.89
            // 0.90 - 0.99
            // 1.00 - 1.40

            SeverityLookup result = null;
            foreach (var severity in orderedSeverities)
            {
                if (severity.MinScore > score)
                    break;

                result = severity;
            }
            return result;
        }
    }
}
