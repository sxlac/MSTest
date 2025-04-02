using MediatR;
using Microsoft.EntityFrameworkCore;
using NewRelic.Api.Agent;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.Queries
{
    public class QueryHold : IRequest<Hold>
    {
        public long EvaluationId { get; init; }
    }

    public class QueryHoldHandler : IRequestHandler<QueryHold, Hold>
    {
        private readonly SpirometryDataContext _context;

        public QueryHoldHandler(SpirometryDataContext context)
        {
            _context = context;
        }

        [Transaction]
        public async Task<Hold> Handle(QueryHold request, CancellationToken cancellationToken)
        {
            return await _context.Holds
                .AsNoTracking()
                .FirstOrDefaultAsync(each => each.EvaluationId == request.EvaluationId, cancellationToken);
        }
    }
}
