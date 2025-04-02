using MediatR;
using Microsoft.EntityFrameworkCore;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Core.Messages.Queries
{
    public class GetHold : IRequest<Hold>
    {
        public long EvaluationId { get; init; }
    }

    public class GetHoldHandler : IRequestHandler<GetHold, Hold>
    {
        private readonly DataContext _context;

        public GetHoldHandler(DataContext context)
        {
            _context = context;
        }

        [Transaction]
        public async Task<Hold> Handle(GetHold request, CancellationToken cancellationToken)
        {
            return await _context.Holds
                .AsNoTracking()
                .FirstOrDefaultAsync(each => each.EvaluationId == request.EvaluationId, cancellationToken);
        }
    }
}
