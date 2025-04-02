using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.HBA1CPOC.Svc.Core.Data;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;

namespace Signify.HBA1CPOC.Svc.Core.Queries;

public class GetRcmBillingByBillId : IRequest<HBA1CPOCRCMBilling>
{
    public string BillId { get; set; }
}

public class GetRcmBillingByBillIdHandler(Hba1CpocDataContext dataContext)
    : IRequestHandler<GetRcmBillingByBillId, HBA1CPOCRCMBilling>
{
    public Task<HBA1CPOCRCMBilling> Handle(GetRcmBillingByBillId request, CancellationToken cancellationToken)
        => dataContext.HBA1CPOCRCMBilling.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BillId == request.BillId, cancellationToken);
}