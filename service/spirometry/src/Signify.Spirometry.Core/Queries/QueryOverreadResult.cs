using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.Queries
{
    public class QueryOverreadResult : IRequest<OverreadResult>
    {
        public long AppointmentId { get; }

        public QueryOverreadResult(long appointmentId)
        {
            AppointmentId = appointmentId;
        }
    }

    public class QueryOverreadResultHandler : IRequestHandler<QueryOverreadResult, OverreadResult>
    {
        private readonly SpirometryDataContext _dataContext;

        public QueryOverreadResultHandler(SpirometryDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public Task<OverreadResult> Handle(QueryOverreadResult request, CancellationToken cancellationToken)
        {
            return _dataContext.OverreadResults
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.AppointmentId == request.AppointmentId, cancellationToken);
        }
    }
}
