using MediatR;
using Signify.A1C.Svc.Core.ApiClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NewRelic.Api.Agent;

namespace Signify.A1C.Svc.Core.Queries
{
    public class CheckA1CEval : IRequest<string>
    {
        public int EvaluationId { get; set; }
    }

    /// <summary>
    /// Check if A1C delivered and barcode exists from Evaluation answers.
    /// </summary>
    public class CheckA1CEvalHandler : IRequestHandler<CheckA1CEval, string>
    {
        private readonly IEvaluationApi _evalApi;

        public CheckA1CEvalHandler(IEvaluationApi evalApi)
        {
            _evalApi = evalApi;
        }

        [Trace]
        public async Task<string> Handle(CheckA1CEval request, CancellationToken cancellationToken)
        {
            var evalVerRs = await _evalApi.GetEvaluationVersion(request.EvaluationId);

            if (!(evalVerRs?.Evaluation?.Answers.Count > 1)) return string.Empty;

            //Check Blood collected today
            var bloodCollectedAns = evalVerRs.Evaluation.Answers.Any(s =>
                s.QuestionId == 464 && s.FormAnswerMeaningId == "464.01" && s.AnswerValue == "1" && s.AnswerId == 20956 );

            if (!bloodCollectedAns) return string.Empty;
            //Check if barcode exist
            var barCodeAns = evalVerRs.Evaluation.Answers.FirstOrDefault(s =>
                s.QuestionId == 566 && s.FormAnswerMeaningId == "566.01" && s.AnswerId == 21118);

            return barCodeAns != null ? barCodeAns.AnswerValue : string.Empty;
        }
    }
}