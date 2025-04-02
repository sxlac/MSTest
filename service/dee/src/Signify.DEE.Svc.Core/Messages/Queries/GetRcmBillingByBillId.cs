using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;

namespace Signify.DEE.Svc.Core.Messages.Queries;

public class GetRcmBillingByBillId(string billId) : IRequest<DEEBilling>
{
    public string BillId { get; } = billId;
}

public class GetRcmBillingByBillIdHandler(DataContext dataContext) : IRequestHandler<GetRcmBillingByBillId, DEEBilling>
{
    public Task<DEEBilling> Handle(GetRcmBillingByBillId id, CancellationToken cancellationToken)
    {
        return dataContext.DEEBilling.AsNoTracking()
            .FirstOrDefaultAsync(x => string.Equals(x.BillId.ToUpper(), id.BillId.ToUpper()), cancellationToken);
    }
}