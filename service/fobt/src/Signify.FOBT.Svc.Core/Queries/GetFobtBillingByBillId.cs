using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Data.Entities;

namespace Signify.FOBT.Svc.Core.Queries;

public class GetFobtBillingByBillId : IRequest<FOBTBilling>
{
    public string BillId { get; }

    public GetFobtBillingByBillId(string billId)
    {
        BillId = billId;
    }
}

public class GetFobtBillingByBillIdHandler : IRequestHandler<GetFobtBillingByBillId, FOBTBilling>
{
    private readonly FOBTDataContext _dataContext;

    public GetFobtBillingByBillIdHandler(FOBTDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public Task<FOBTBilling> Handle(GetFobtBillingByBillId request, CancellationToken cancellationToken)
    {
        return _dataContext.FOBTBilling.AsNoTracking()
            .FirstOrDefaultAsync(x => string.Equals(x.BillId.ToUpper(), request.BillId.ToUpper()), cancellationToken);
    }
}