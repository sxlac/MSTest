using MediatR;
using Signify.Spirometry.Core.ApiClients.EvaluationApi.Responses;
using Signify.Spirometry.Core.Constants.Questions;
using Signify.Spirometry.Core.Exceptions;
using Signify.Spirometry.Core.Models;
using Signify.Spirometry.Core.Queries;
using Signify.Spirometry.Core.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.Commands
{
    /// <summary>
    /// Command to generate a new <see cref="ExamModel"/> object from evaluation answers queried from the Evaluation API
    /// </summary>
    public class GenerateExamModel : IRequest<ExamModel>
    {
        public int EvaluationId { get; }

        public GenerateExamModel(int evaluationId)
        {
            EvaluationId = evaluationId;
        }
    }

    public class GenerateExamModelHandler : IRequestHandler<GenerateExamModel, ExamModel>
    {
        private readonly IMediator _mediator;
        private readonly IBuildAnswerLookup _lookupBuilder;

        public GenerateExamModelHandler(IMediator mediator, IBuildAnswerLookup lookupBuilder)
        {
            _mediator = mediator;
            _lookupBuilder = lookupBuilder;
        }

        public async Task<ExamModel> Handle(GenerateExamModel request, CancellationToken cancellationToken)
        {
            var evaluationModel = await _mediator.Send(new QueryEvaluationModel(request.EvaluationId), cancellationToken).ConfigureAwait(false);

            var answerLookup = _lookupBuilder.BuildLookup(evaluationModel.Answers);

            const int questionId = SpirometryTestPerformedQuestion.QuestionId;

            var testPerformedQ = GetRequired(answerLookup, questionId);

            ExamModel model = testPerformedQ.AnswerId switch
            {
                SpirometryTestPerformedQuestion.YesAnswerId
                    => await _mediator.Send(new ParsePerformedResults(request.EvaluationId, answerLookup), cancellationToken),
                SpirometryTestPerformedQuestion.NoAnswerId
                    => await _mediator.Send(new ParseNotPerformedResults(request.EvaluationId, answerLookup), cancellationToken),
                _ => throw new UnsupportedAnswerForQuestionException(questionId, testPerformedQ.AnswerId, testPerformedQ.AnswerValue)
            };

            model.FormVersionId = evaluationModel.FormVersionId;

            return model;
        }

        /// <summary>
        /// Gets the answer to a required question that we cannot move forward without having the answer
        /// </summary>
        /// <exception cref="RequiredEvaluationQuestionMissingException"></exception>
        private static EvaluationAnswerModel GetRequired(IReadOnlyDictionary<int, ICollection<EvaluationAnswerModel>> allAnswers, int questionId)
        {
            if (!allAnswers.TryGetValue(questionId, out var questionAnswers))
                throw new RequiredEvaluationQuestionMissingException(questionId);

            return questionAnswers.First();
        }
    }
}
