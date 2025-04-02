using MediatR;
using Microsoft.Extensions.Logging;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Svc.Core.ApiClient.Response;
using Signify.PAD.Svc.Core.Constants;
using Signify.PAD.Svc.Core.Constants.Questions.NotPerformed;
using Signify.PAD.Svc.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Signify.PAD.Svc.Core.Constants.Observability;

namespace Signify.PAD.Svc.Core.Commands
{
    /// <summary>
    /// Command to parse evaluation answers where a PAD exam was not performed
    /// </summary>
    public class ParseNotPerformedResults : ParseResultsBase, IRequest<EvaluationAnswers>
    {
        public ParseNotPerformedResults(long evaluationId, IReadOnlyDictionary<int, ICollection<EvaluationAnswerModel>> answers)
            : base(evaluationId, answers)
        {
        }
    }

    public class ParseNotPerformedResultsHandler : ParseResultsHandlerBase, IRequestHandler<ParseNotPerformedResults, EvaluationAnswers>
    {
        private readonly IPublishObservability _publishObservability;

        private const string NotPerformedRefusedCaption = "Member refused"; //"Member Refused Type String is Constant"
        private const string NotPerformedUnableCaption = "Unable to perform"; //"Member Refused Type String is Constant"
        private const string NotClinicallyRelevantCaption = "Not clinically relevant";

        private const int NotesMaxLength = 1024; // Notes length shouldn't be greater than 1024
        private const int TechnicalIssueAnswerId = 30966;

        private enum ReasonNotPerformed
        {
            MemberRefused,
            UnableToPerform,
            NotClinicallyRelevant
        }

        public ParseNotPerformedResultsHandler(ILogger<ParseNotPerformedResultsHandler> logger, IPublishObservability publishObservability)
            : base(logger)
        {
            _publishObservability = publishObservability;
        }

        public Task<EvaluationAnswers> Handle(ParseNotPerformedResults request, CancellationToken cancellationToken)
        {
            var evaluationId = request.EvaluationId;
            var allAnswers = request.Answers;
            var result = new EvaluationAnswers
            {
                IsPadPerformedToday = false
            };

            if (!TryGetReasonNotPerformed(evaluationId, allAnswers, out var reason))
            {
                Logger.LogWarning("Evaluation with a PAD product did not have a PAD test performed today, but no reason why it was not performed was found, for EvaluationId={EvaluationId}",
                    evaluationId);
                return Task.FromResult(result);
            }

            NotPerformedReason GetSubReason(int questionId, IEnumerable<NotPerformedReason> possibleReasons)
            {
                if (!allAnswers.TryGetValue(questionId, out var answerModels))
                {
                    Logger.LogWarning("Unable to determine the sub-reason why a PAD test was not performed, because QuestionId={QuestionId} was not answered, for EvaluationId={EvaluationId}",
                        questionId, evaluationId);
                    return null;
                }

                var answer =
                (
                    from reasons in possibleReasons
                    join answers in answerModels on reasons.AnswerId equals answers.AnswerId
                    select reasons
                ).FirstOrDefault();

                if (answer == null)
                {
                    Logger.LogWarning("Unable to determine the sub-reason why a PAD test was not performed, because none of the answers ({AnswerIds}) to QuestionId={QuestionId} are supported, for EvaluationId={EvaluationId}",
                        string.Join(',', answerModels.Select(a => a.AnswerId)), questionId, evaluationId);
                }
                else
                {
                    if (answer.AnswerId == TechnicalIssueAnswerId)
                    {
                        var observabilityTechnicalIssueEvent = new ObservabilityEvent
                        {
                            EvaluationId = evaluationId,
                            EventType = ParseNotPerformedResult.TechnicalIssue,
                            EventValue = new Dictionary<string, object>
                            {
                                { EventParams.EvaluationId, evaluationId },
                                { EventParams.CreatedDateTime, DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
                            }
                        };

                        _publishObservability.RegisterEvent(observabilityTechnicalIssueEvent, true);
                    }
                }

                return answer;
            }

            NotPerformedReason subReason;
            int notesQuestionId, notesAnswerId;
            switch (reason)
            {
                case ReasonNotPerformed.MemberRefused:
                    subReason = GetSubReason(ReasonMemberRefusedQuestion.QuestionId, Application.NotPerformedRefused);

                    result.NotPerformedReasonType = NotPerformedRefusedCaption;

                    notesQuestionId = MemberRefusedNotesQuestion.QuestionId;
                    notesAnswerId = MemberRefusedNotesQuestion.AnswerId;
                    break;
                case ReasonNotPerformed.UnableToPerform:
                    subReason = GetSubReason(ReasonUnableToPerformQuestion.QuestionId, Application.NotPerformedUnable);

                    result.NotPerformedReasonType = NotPerformedUnableCaption;

                    notesQuestionId = UnableToPerformNotesQuestion.QuestionId;
                    notesAnswerId = UnableToPerformNotesQuestion.AnswerId;
                    break;
                case ReasonNotPerformed.NotClinicallyRelevant:
                    subReason = GetSubReason(ReasonNotPerformedQuestion.QuestionId, Application.NotPerformedUnable);

                    result.NotPerformedReasonType = NotClinicallyRelevantCaption;

                    notesQuestionId = ReasonNotPerformedQuestion.QuestionId;
                    notesAnswerId = ReasonNotPerformedQuestion.ReasonNotClinicallyRelevantNotesAnswerId;
                    break;
                default:
                    throw new NotImplementedException("Should not be possible to get here; enum defined private in this class has been modified, but this switch hasn't been expanded");
            }

            if (subReason != null)
            {
                result.NotPerformedAnswerId = subReason.AnswerId;
                result.NotPerformedReason = subReason.Reason;
            }

            result.NotPerformedNotes = GetNotPerformedNotes(allAnswers, notesQuestionId, notesAnswerId);

            if (string.IsNullOrEmpty(result.NotPerformedNotes))
                Logger.LogInformation("Provider did not provide not performed notes, for EvaluationId={EvaluationId}", evaluationId);

            return Task.FromResult(result);
        }

        private bool TryGetReasonNotPerformed(long evaluationId, IReadOnlyDictionary<int, ICollection<EvaluationAnswerModel>> allAnswers, out ReasonNotPerformed reason)
        {
            const int questionId = ReasonNotPerformedQuestion.QuestionId;

            if (!allAnswers.TryGetValue(questionId, out var answers))
            {
                Logger.LogWarning("Unable to determine the reason why a PAD test was not performed, because the question ({QuestionId}) was not answered, for EvaluationId={EvaluationId}",
                    questionId, evaluationId);
                reason = default;
                return false;
            }

            foreach (var answer in answers)
            {
                switch (answer.AnswerId)
                {
                    case ReasonNotPerformedQuestion.MemberRefusedAnswerId:
                        reason = ReasonNotPerformed.MemberRefused; return true;
                    case ReasonNotPerformedQuestion.UnableToPerformAnswerId:
                        reason = ReasonNotPerformed.UnableToPerform; return true;
                    case ReasonNotPerformedQuestion.NotClinicallyRelevantAnswerId:
                        reason = ReasonNotPerformed.NotClinicallyRelevant; return true;
                }
            }

            Logger.LogWarning("Unable to determine the reason why a PAD test was not performed, because none of the answers ({AnswerIds}) are in the list of supported reasons, for EvaluationId={EvaluationId}",
                string.Join(',', answers.Select(a => a.AnswerId)), evaluationId);

            reason = default;
            return false;
        }

        private static string GetNotPerformedNotes(IReadOnlyDictionary<int, ICollection<EvaluationAnswerModel>> allAnswers, int questionId, int answerId)
        {
            if (!allAnswers.TryGetValue(questionId, out var answers))
                return string.Empty;

            var notesAnswer = answers.FirstOrDefault(a => a.AnswerId == answerId);
            if (notesAnswer?.AnswerValue == null)
                return string.Empty;

            return notesAnswer.AnswerValue.Length > NotesMaxLength
                ? notesAnswer.AnswerValue[..NotesMaxLength]
                : notesAnswer.AnswerValue;
        }
    }
}
