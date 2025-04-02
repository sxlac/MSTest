using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.CKD.Svc.Core.Data;
using Signify.CKD.Svc.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.CKD.Svc.Core.Queries
{
    public class GetRcmBilling : IRequest<CKDRCMBilling>
    {
        public int CKDId { get; set; }
    }

    public class GetRcmBillingHandler : IRequestHandler<GetRcmBilling, CKDRCMBilling>
    {
        private readonly CKDDataContext _dataContext;

        public GetRcmBillingHandler(CKDDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public Task<CKDRCMBilling> Handle(GetRcmBilling request, CancellationToken cancellationToken)
        {
            return _dataContext.CKDRCMBilling
                .AsNoTracking().FirstOrDefaultAsync(x => x.CKDId == request.CKDId, cancellationToken);
        }
    }
}
