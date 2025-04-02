using MediatR;
using Microsoft.Extensions.Logging;
using Signify.uACR.Core.ApiClients.EvaluationApi;
using Signify.uACR.Core.ApiClients.EvaluationApi.Responses;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.uACR.Core.Queries;

public class QueryEvaluationModel(long evaluationId) : IRequest<EvaluationModel>
{
    public long EvaluationId { get; } = evaluationId;
}

public class QueryEvaluationModelHandler(ILogger<QueryEvaluationModelHandler> logger, IEvaluationApi evaluationApi)
    : IRequestHandler<QueryEvaluationModel, EvaluationModel>
{
    public async Task<EvaluationModel> Handle(QueryEvaluationModel request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Querying Evaluation API for evaluation version for EvaluationId={EvaluationId}",
            request.EvaluationId);

        var evaluation = (await evaluationApi.GetEvaluationVersion(request.EvaluationId).ConfigureAwait(false)).Evaluation;

        logger.LogInformation("Received evaluation version from Evaluation API for EvaluationId={EvaluationId} with {AnswerCount} answers",
            request.EvaluationId, evaluation.Answers.Count);

        return evaluation;
    }
}