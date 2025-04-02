using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.uACR.Core.Data;
using Signify.uACR.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.uACR.Core.Queries;

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