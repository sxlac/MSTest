using MediatR;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.ApiClients.EvaluationApi;
using Signify.Spirometry.Core.ApiClients.EvaluationApi.Responses;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.Queries
{
    public class QueryEvaluationModel : IRequest<EvaluationModel>
    {
        public int EvaluationId { get; }

        public QueryEvaluationModel(int evaluationId)
        {
            EvaluationId = evaluationId;
        }
    }

    public class QueryEvaluationModelHandler : IRequestHandler<QueryEvaluationModel, EvaluationModel>
    {
        private readonly ILogger _logger;
        private readonly IEvaluationApi _evaluationApi;

        public QueryEvaluationModelHandler(ILogger<QueryEvaluationModelHandler> logger, IEvaluationApi evaluationApi)
        {
            _logger = logger;
            _evaluationApi = evaluationApi;
        }

        public async Task<EvaluationModel> Handle(QueryEvaluationModel request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Querying Evaluation API for evaluation version for EvaluationId={EvaluationId}",
                request.EvaluationId);

            var evaluationVersion = await _evaluationApi.GetEvaluationVersion(request.EvaluationId);

            var evaluation = evaluationVersion.Evaluation;

            _logger.LogInformation("Received evaluation version {Version} from the Evaluation API for EvaluationId={EvaluationId} on FormVersionId={FormVersionId} with {AnswerCount} answers",
                evaluationVersion.Version, request.EvaluationId, evaluation.FormVersionId, evaluation.Answers.Count);

            return evaluation;
        }
    }
}
