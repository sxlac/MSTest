using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.PAD.Svc.Core.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.PAD.Svc.Core.Queries
{
    /// <summary>
    /// Query to determine whether or not a billing request was created
    /// </summary>
    public class QueryBillRequestSent : IRequest<bool>
    {
        public int PadId { get; }

        public QueryBillRequestSent(int padId)
        {
            PadId = padId;
        }
    }

    public class QueryBillRequestSentHandler : IRequestHandler<QueryBillRequestSent, bool>
    {
        private readonly PADDataContext _dataContext;

        public QueryBillRequestSentHandler(PADDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public Task<bool> Handle(QueryBillRequestSent request, CancellationToken cancellationToken)
        {
            return _dataContext.PADRCMBilling
                .AsNoTracking()
                .Include(each => each.PAD)
                .AnyAsync(each => each.PAD.PADId == request.PadId, cancellationToken);
        }
    }
}
