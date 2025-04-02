using System;
using MediatR;
using Signify.HBA1CPOC.Svc.Core.ApiClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NewRelic.Api.Agent;
using Signify.HBA1CPOC.Svc.Core.Models;
using Signify.HBA1CPOC.Svc.Core.Constants.Questions;
using Signify.HBA1CPOC.Svc.Core.Constants.Questions.Performed;

namespace Signify.HBA1CPOC.Svc.Core.Queries
{
    public class CheckHBA1CPOCEval : IRequest<EvaluationAnswers>
    {
        public long EvaluationId { get; set; }
    }

    /// <summary>
    /// Check if HBA1CPOC delivered and barcode exists from Evaluation answers.
    /// </summary>
    public class CheckHBA1CPOCEvalHandler : IRequestHandler<CheckHBA1CPOCEval, EvaluationAnswers>
    {
        private readonly IEvaluationApi _evalApi;
        private const string yesAnswerValue = "1";

        public CheckHBA1CPOCEvalHandler(IEvaluationApi evalApi)
        {
            _evalApi = evalApi;
        }

        [Trace]
        public async Task<EvaluationAnswers> Handle(CheckHBA1CPOCEval request, CancellationToken cancellationToken)
        {
            EvaluationAnswers evaluation = new EvaluationAnswers();
            var evaluationVersionResponse = await _evalApi.GetEvaluationVersion((int)request.EvaluationId);

            if (evaluationVersionResponse?.Evaluation == null || evaluationVersionResponse.Evaluation?.Answers.Count < 1)
                return evaluation;

            /*************Remove hardcoded values *****************/
            //Check Blood collected today and A1C POC percent is not blank
            evaluation.IsHBA1CEvaluation = evaluationVersionResponse.Evaluation.Answers.Any(s => s.QuestionId == HbA1CPocTestPerformedQuestion.QuestionId &&
                                                                                    s.AnswerValue == yesAnswerValue &&
                                                                                    s.AnswerId == HbA1CPocTestPerformedQuestion.YesAnswerId) &&
                                           evaluationVersionResponse.Evaluation.Answers.Any(s => s.QuestionId == PercentA1CQuestion.QuestionId && 
                                                                                    !string.IsNullOrEmpty(s.AnswerValue) && 
                                                                                    s.AnswerId == PercentA1CQuestion.AnswerId) &&
                                           evaluationVersionResponse.Evaluation.Answers.Any(s => s.QuestionId == ExpirationDateQuestion.QuestionId && 
                                                                                    !string.IsNullOrEmpty(s.AnswerValue) && 
                                                                                    s.AnswerId == ExpirationDateQuestion.AnswerId);

            if (!evaluation.IsHBA1CEvaluation)
                return evaluation;

            evaluation.A1CPercent = evaluationVersionResponse.Evaluation.Answers.FirstOrDefault(s => s.QuestionId == PercentA1CQuestion.QuestionId &&
                                                                                        !string.IsNullOrEmpty(s.AnswerValue) &&
                                                                                        s.AnswerId == PercentA1CQuestion.AnswerId)?.AnswerValue;


            var expirationDateAnswer = Convert.ToDateTime(evaluationVersionResponse.Evaluation.Answers.FirstOrDefault(s => s.QuestionId == ExpirationDateQuestion.QuestionId && 
                                                                                                !string.IsNullOrEmpty(s.AnswerValue) && 
                                                                                                s.AnswerId == ExpirationDateQuestion.AnswerId)?.AnswerValue);

            evaluation.ExpirationDate = DateTime.SpecifyKind(expirationDateAnswer, DateTimeKind.Utc);

            return evaluation;
        }
    }
}