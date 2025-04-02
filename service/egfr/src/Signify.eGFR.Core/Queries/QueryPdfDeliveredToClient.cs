using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.eGFR.Core.Queries;

/// <summary>
/// Query to search for <see cref="PdfDeliveredToClient"/> by a given EvaluationId, if one exists
/// </summary>
public class QueryPdfDeliveredToClient(long evaluationId) : IRequest<QueryPdfDeliveredToClientResult>
{
    public long EvaluationId { get; } = evaluationId;
}

public class QueryPdfDeliveredToClientResult(PdfDeliveredToClient entity)
{
    public PdfDeliveredToClient Entity { get; } = entity;
}

public class QueryPdfDeliveredToClientHandler(DataContext dataContext)
    : IRequestHandler<QueryPdfDeliveredToClient, QueryPdfDeliveredToClientResult>
{
    public async Task<QueryPdfDeliveredToClientResult> Handle(QueryPdfDeliveredToClient request, CancellationToken cancellationToken)
    {
        var entity = await dataContext.PdfDeliveredToClients
            .AsNoTracking()
            .FirstOrDefaultAsync(each => each.EvaluationId == request.EvaluationId, cancellationToken);

        return new QueryPdfDeliveredToClientResult(entity);
    }
}