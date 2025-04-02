using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewRelic.Api.Agent;
using Signify.CKD.Svc.Core.Data;
using Signify.CKD.Svc.Core.Data.Entities;

namespace Signify.CKD.Svc.Core.Queries
{
    public class GetCKDStatuses : IRequest<IList<CKDStatus>>
    {
        public int CKDId { get; set; }
    }

    /// <summary>
    /// Get CKD status details from database.
    /// </summary>
    public class GetCKDStatusesHandler : IRequestHandler<GetCKDStatuses, IList<CKDStatus>>
    {
        private readonly CKDDataContext _dataContext;
        public GetCKDStatusesHandler(CKDDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        [Trace]
        public async Task<IList<CKDStatus>> Handle(GetCKDStatuses request, CancellationToken cancellationToken)
        {
            return await _dataContext.CKDStatus
                .AsNoTracking()
                .Include(s => s.CKD)
                .Include(c => c.CKDStatusCode)
                .Where(s => s.CKD.CKDId == request.CKDId)
                .ToListAsync(cancellationToken);
        }
    }
}
