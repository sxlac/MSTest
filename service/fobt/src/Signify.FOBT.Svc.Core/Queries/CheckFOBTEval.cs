using MediatR;
using Signify.FOBT.Svc.Core.ApiClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NewRelic.Api.Agent;

namespace Signify.FOBT.Svc.Core.Queries
{
    public class CheckFOBTEval : IRequest<string>
    {
        public int EvaluationId { get; set; }
    }

    /// <summary>
    /// Check if FOBT delivered and barcode exists from Evaluation answers.
    /// </summary>
    public class CheckFOBTEvalHandler : IRequestHandler<CheckFOBTEval, string>
    {
        private readonly IEvaluationApi _evalApi;

        public enum QuestionAnswer
        {
            QuestionIdYes = 564,
            AnswerIdYes = 21113,
            QuestionIdBarcode = 567,
            AnswerIdBarcode = 21119
           
        }

        public CheckFOBTEvalHandler(IEvaluationApi evalApi)
        {
            _evalApi = evalApi;
        }

        [Trace]
        public async Task<string> Handle(CheckFOBTEval request, CancellationToken token)
        {
            var evalVerRs = await _evalApi.GetEvaluationVersion(request.EvaluationId);

            if (!(evalVerRs?.Evaluation?.Answers.Count > 1)) return string.Empty;
            /*************TODO: Remove hardcoded values *****************/
            //Check FOBT  collected today
            var fobtCollectedAns = evalVerRs.Evaluation.Answers.Any(s =>  s.QuestionId == (int)QuestionAnswer.QuestionIdYes && s.AnswerId == (int)QuestionAnswer.AnswerIdYes && !string.IsNullOrEmpty(s.AnswerValue) );
          
            if (!fobtCollectedAns) return string.Empty;

            //Check if barcode exist
            var barCodeAns = evalVerRs.Evaluation.Answers.FirstOrDefault(s => s.QuestionId == (int)QuestionAnswer.QuestionIdBarcode && s.AnswerId == (int)QuestionAnswer.AnswerIdBarcode);
            return barCodeAns != null ? barCodeAns.AnswerValue : string.Empty;
        }
    }
}