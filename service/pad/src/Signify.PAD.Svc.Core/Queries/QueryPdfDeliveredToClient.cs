using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.Data.Entities;

namespace Signify.PAD.Svc.Core.Queries;

/// <summary>
/// Query to search for <see cref="PDFToClient"/> by a given EvaluationId, if one exists
/// </summary>
public class QueryPdfDeliveredToClient : IRequest<QueryPdfDeliveredToClientResult>
{
    public long EvaluationId { get; }

    public QueryPdfDeliveredToClient(long evaluationId)
    {
        EvaluationId = evaluationId;
    }
}

/// <summary>
/// Response to the <see cref="QueryPdfDeliveredToClient"/> query
/// </summary>
public class QueryPdfDeliveredToClientResult
{
    public PDFToClient Entity { get; }

    public QueryPdfDeliveredToClientResult(PDFToClient entity)
    {
        Entity = entity;
    }
}

public class QueryPdfDeliveredToClientHandler : IRequestHandler<QueryPdfDeliveredToClient, QueryPdfDeliveredToClientResult>
{
    private readonly PADDataContext _context;

    public QueryPdfDeliveredToClientHandler(PADDataContext context)
    {
        _context = context;
    }

    public async Task<QueryPdfDeliveredToClientResult> Handle(QueryPdfDeliveredToClient request, CancellationToken cancellationToken)
    {
        var entity = await _context.PDFToClient
            .AsNoTracking()
            .FirstOrDefaultAsync(each => each.EvaluationId == request.EvaluationId, cancellationToken);

        return new QueryPdfDeliveredToClientResult(entity);
    }
}