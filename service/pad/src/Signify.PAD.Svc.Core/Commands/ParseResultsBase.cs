using Microsoft.Extensions.Logging;
using Signify.PAD.Svc.Core.ApiClient.Response;
using System.Collections.Generic;
using System.Linq;

namespace Signify.PAD.Svc.Core.Commands
{
    /// <summary>
    /// Abstract base for a command to parse evaluation answers into a concrete data model
    /// </summary>
    public abstract class ParseResultsBase
    {
        /// <summary>
        /// Identifier of the evaluation
        /// </summary>
        public long EvaluationId { get; }

        /// <summary>
        /// Lookup of Questions to Answers
        /// </summary>
        /// <remarks>
        /// Key: QuestionId<br />
        /// Value: Collection of one or more answers to the given question
        /// </remarks>
        public IReadOnlyDictionary<int, ICollection<EvaluationAnswerModel>> Answers { get; }

        /// <param name="evaluationId">Identifier of the evaluation</param>
        /// <param name="answers">Lookup of Questions to Answers</param>
        protected ParseResultsBase(long evaluationId, IReadOnlyDictionary<int, ICollection<EvaluationAnswerModel>> answers)
        {
            EvaluationId = evaluationId;
            Answers = answers;
        }
    }

    public abstract class ParseResultsHandlerBase
    {
        protected ILogger Logger { get; }

        protected ParseResultsHandlerBase(ILogger logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Attempts to get the answer to an expected (but not required) question
        /// </summary>
        protected bool TryGetExpected(ParseResultsBase request, int questionId, string questionName, out EvaluationAnswerModel answerModel)
        {
            if (TryGetOptional(request, questionId, out answerModel))
                return true;

            Logger.LogWarning("For EvaluationId={EvaluationId}, required {QuestionName} question/answer is missing - QuestionId={QuestionId}",
                request.EvaluationId, questionName, questionId);

            return false;
        }

        /// <summary>
        /// Attempts to get the answer to an optional question
        /// </summary>
        protected static bool TryGetOptional(ParseResultsBase request, int questionId, out EvaluationAnswerModel answerModel)
        {
            if (request.Answers.TryGetValue(questionId, out var answers))
            {
                answerModel = answers.First();
                return true;
            }

            answerModel = default;
            return false;
        }
    }
}
