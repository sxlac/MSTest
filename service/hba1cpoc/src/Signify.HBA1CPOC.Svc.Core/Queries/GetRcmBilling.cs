using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.HBA1CPOC.Svc.Core.Data;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.HBA1CPOC.Svc.Core.Queries
{
    public class GetRcmBilling : IRequest<HBA1CPOCRCMBilling>
    {
        public int HbA1cPocId { get; set; }
    }

    public class GetRcmBillingHandler : IRequestHandler<GetRcmBilling, HBA1CPOCRCMBilling>
    {
        private readonly Hba1CpocDataContext _dataContext;

        public GetRcmBillingHandler(Hba1CpocDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public Task<HBA1CPOCRCMBilling> Handle(GetRcmBilling request, CancellationToken cancellationToken)
        {
            return _dataContext.HBA1CPOCRCMBilling.AsNoTracking().FirstOrDefaultAsync(x => x.HBA1CPOCId == request.HbA1cPocId, cancellationToken);
        }
    }
}
