using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.ApiClients.EvaluationApi.Responses;
using Signify.Spirometry.Core.Exceptions;
using Signify.Spirometry.Core.Factories;
using Signify.Spirometry.Core.Models;
using System.Collections.Generic;
using System.Linq;
using TCT = Signify.Spirometry.Core.Factories.ITrileanTypeConverterFactory.TrileanConverterType;

namespace Signify.Spirometry.Core.Commands
{
    /// <summary>
    /// Abstract base for a command to parse evaluation answers into a concrete data model
    /// </summary>
    public abstract class ParseResultsBase
    {
        /// <summary>
        /// Identifier of the evaluation
        /// </summary>
        public int EvaluationId { get; }

        /// <summary>
        /// Lookup where the keys are Question ID's and values are the answer values
        /// </summary>
        public IReadOnlyDictionary<int, ICollection<EvaluationAnswerModel>> Answers { get; }

        /// <param name="evaluationId">Identifier of the evaluation</param>
        /// <param name="answers">Lookup where the keys are Question ID's and the values are the answer values</param>
        protected ParseResultsBase(int evaluationId, IReadOnlyDictionary<int, ICollection<EvaluationAnswerModel>> answers)
        {
            EvaluationId = evaluationId;
            Answers = answers;
        }
    }

    public abstract class ParseResultsHandlerBase
    {
        protected ILogger Logger { get; }

        protected ITrileanTypeConverterFactory TrileanConverterFactory { get; }

        protected ParseResultsHandlerBase(ILogger logger, ITrileanTypeConverterFactory trileanConverterFactory)
        {
            Logger = logger;
            TrileanConverterFactory = trileanConverterFactory;
        }

        /// <summary>
        /// Attempts to get the answer to the optional question supplied, parsed as a <see cref="TrileanType"/>
        /// </summary>
        /// <returns>
        /// <c>true</c> if the question was answered and the value can be parsed to a <see cref="TrileanType"/>,
        /// -or-
        /// <c>false</c> if the question was not answered
        /// </returns>
        /// <exception cref="UnsupportedAnswerForQuestionException">
        /// Thrown if the question was answered, but the answer value is not supported for this question
        /// </exception>
        protected bool TryParseTrilean(ParseResultsBase request, int questionId, TCT converterType, out TrileanType? trileanType)
        {
            if (!TryGetOptional(request, questionId, out var answerModel))
            {
                trileanType = null;
                return false;
            }

            var converter = TrileanConverterFactory.Create(converterType);

            if (!converter.TryConvert(answerModel.AnswerId, out var tt))
                throw new UnsupportedAnswerForQuestionException(questionId, answerModel.AnswerId, answerModel.AnswerValue);

            trileanType = tt;
            return true;
        }

        /// <summary>
        /// Gets the answer to a required question that we cannot move forward without having the answer
        /// </summary>
        /// <exception cref="RequiredEvaluationQuestionMissingException"></exception>
        protected static EvaluationAnswerModel GetRequired(ParseResultsBase request, int questionId)
        {
            if (!TryGetOptional(request, questionId, out var answerModel))
                throw new RequiredEvaluationQuestionMissingException(questionId);

            return answerModel;
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
