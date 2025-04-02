using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.uACR.Core.ApiClients.EvaluationApi;

namespace Signify.uACR.Core.Queries;

public class QueryEvaluationProductCodes(long evaluationId) : IRequest<List<string>>
{
    public long EvaluationId { get; } = evaluationId;
}

public class QueryEvaluationProductCodesHandler(
    ILogger<QueryEvaluationProductCodesHandler> logger,
    IEvaluationApi evaluationApi)
    : IRequestHandler<QueryEvaluationProductCodes, List<string>>
{
    public async Task<List<string>> Handle(QueryEvaluationProductCodes request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Querying Evaluation API for a list of products for EvaluationId={EvaluationId}",
            request.EvaluationId);

        var productCodes = (await evaluationApi.GetEvaluationProductCodes(request.EvaluationId).ConfigureAwait(false)).Content.ProductCodes;

        logger.LogInformation("Received a list of products from Evaluation API for EvaluationId={EvaluationId}",
            request.EvaluationId);

        return productCodes;
    }
}