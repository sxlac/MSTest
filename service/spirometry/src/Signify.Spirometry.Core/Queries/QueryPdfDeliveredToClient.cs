using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.Queries
{
    /// <summary>
    /// Query to search for <see cref="PdfDeliveredToClient"/> by a given EvaluationId, if one exists
    /// </summary>
    public class QueryPdfDeliveredToClient : IRequest<QueryPdfDeliveredToClientResult>
    {
        public long EvaluationId { get; }

        public QueryPdfDeliveredToClient(long evaluationId)
        {
            EvaluationId = evaluationId;
        }
    }

    public class QueryPdfDeliveredToClientResult
    {
        public PdfDeliveredToClient Entity { get; }

        public QueryPdfDeliveredToClientResult(PdfDeliveredToClient entity)
        {
            Entity = entity;
        }
    }

    public class QueryPdfDeliveredToClientHandler : IRequestHandler<QueryPdfDeliveredToClient, QueryPdfDeliveredToClientResult>
    {
        private readonly SpirometryDataContext _dataContext;

        public QueryPdfDeliveredToClientHandler(SpirometryDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<QueryPdfDeliveredToClientResult> Handle(QueryPdfDeliveredToClient request, CancellationToken cancellationToken)
        {
            var entity = await _dataContext.PdfDeliveredToClients
                .AsNoTracking()
                .FirstOrDefaultAsync(each => each.EvaluationId == request.EvaluationId, cancellationToken);

            return new QueryPdfDeliveredToClientResult(entity);
        }
    }
}
