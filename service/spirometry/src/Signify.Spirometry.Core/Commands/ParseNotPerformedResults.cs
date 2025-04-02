using MediatR;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.ApiClients.EvaluationApi.Responses;
using Signify.Spirometry.Core.Constants.Questions.NotPerformed;
using Signify.Spirometry.Core.Exceptions;
using Signify.Spirometry.Core.Factories;
using Signify.Spirometry.Core.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.Commands
{
    /// <summary>
    /// Command to parse evaluation answers where a Spirometry exam was not performed
    /// </summary>
    public class ParseNotPerformedResults : ParseResultsBase, IRequest<NotPerformedExamModel>
    {
        public ParseNotPerformedResults(int evaluationId, IReadOnlyDictionary<int, ICollection<EvaluationAnswerModel>> answers)
            : base(evaluationId, answers)
        {
        }
    }

    public class ParseNotPerformedResultsHandler : ParseResultsHandlerBase,
        IRequestHandler<ParseNotPerformedResults, NotPerformedExamModel>
    {
        private const int MaxNotPerformedNotesLength = 1024;

        public ParseNotPerformedResultsHandler(ILogger<ParseNotPerformedResultsHandler> logger,
            ITrileanTypeConverterFactory trileanConverterFactory)
            : base(logger, trileanConverterFactory)
        {
        }

        public Task<NotPerformedExamModel> Handle(ParseNotPerformedResults request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new NotPerformedExamModel(request.EvaluationId, ParseResults(request)));
        }

        private static NotPerformedInfo ParseResults(ParseResultsBase request)
        {
            const int questionId = ReasonNotPerformedQuestion.QuestionId;

            var q = GetRequired(request, questionId);

            var reason = q.AnswerId switch
            {
                ReasonNotPerformedQuestion.MemberRefusedAnswerId => GetMemberRefusedResult(request),
                ReasonNotPerformedQuestion.UnableToPerformAnswerId => GetUnableToPerformResult(request),
                _ => throw new UnsupportedAnswerForQuestionException(questionId, q.AnswerId, q.AnswerValue)
            };

            if (!TryGetOptional(request, NotPerformedNotesQuestion.QuestionId, out var answerModel))
                return new NotPerformedInfo(reason);

            if (answerModel.AnswerId != NotPerformedNotesQuestion.AnswerId)
                throw new UnsupportedAnswerForQuestionException(answerModel.QuestionId, answerModel.AnswerId, answerModel.AnswerValue);

            if (answerModel.AnswerValue == null)
                return new NotPerformedInfo(reason);

            var notes = answerModel.AnswerValue.Length < MaxNotPerformedNotesLength
                ? answerModel.AnswerValue
                : answerModel.AnswerValue.Substring(0, MaxNotPerformedNotesLength);

            return new NotPerformedInfo(reason, notes);
        }

        private static NotPerformedReason GetMemberRefusedResult(ParseResultsBase request)
        {
            const int questionId = ReasonMemberRefusedQuestion.QuestionId;

            var q = GetRequired(request, questionId);

            return q.AnswerId switch
            {
                ReasonMemberRefusedQuestion.MemberRecentlyCompletedAnswerId => NotPerformedReason.MemberRecentlyCompleted,
                ReasonMemberRefusedQuestion.ScheduledToCompleteAnswerId => NotPerformedReason.ScheduledToComplete,
                ReasonMemberRefusedQuestion.MemberApprehensionAnswerId => NotPerformedReason.MemberApprehension,
                ReasonMemberRefusedQuestion.NotInterestedAnswerId => NotPerformedReason.NotInterested,
                _ => throw new UnsupportedAnswerForQuestionException(questionId, q.AnswerId, q.AnswerValue)
            };
        }

        private static NotPerformedReason GetUnableToPerformResult(ParseResultsBase request)
        {
            const int questionId = ReasonUnableToPerformQuestion.QuestionId;

            var q = GetRequired(request, questionId);

            return q.AnswerId switch
            {
                ReasonUnableToPerformQuestion.TechnicalIssueAnswerId => NotPerformedReason.TechnicalIssue,
                ReasonUnableToPerformQuestion.EnvironmentalIssueAnswerId => NotPerformedReason.EnvironmentalIssue,
                ReasonUnableToPerformQuestion.NoSuppliesOrEquipmentAnswerId => NotPerformedReason.NoSuppliesOrEquipment,
                ReasonUnableToPerformQuestion.InsufficientTrainingAnswerId => NotPerformedReason.InsufficientTraining,
                ReasonUnableToPerformQuestion.MemberPhysicallyUnableAnswerId => NotPerformedReason.MemberPhysicallyUnable,
                ReasonUnableToPerformQuestion.MemberOutsideDemographicRangesAnswerId => NotPerformedReason.MemberOutsideDemographicRanges,
                _ => throw new UnsupportedAnswerForQuestionException(questionId, q.AnswerId, q.AnswerValue)
            };
        }
    }
}
