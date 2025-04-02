using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.Data.Entities;

namespace Signify.PAD.Svc.Core.Queries;

public class GetRcmBillingByBillId : IRequest<PADRCMBilling>
{
    public string BillId { get; }

    public GetRcmBillingByBillId(string billId)
    {
        BillId = billId;
    }
}

public class GetRcmBillingByBillIdHandler : IRequestHandler<GetRcmBillingByBillId, PADRCMBilling>
{
    private readonly PADDataContext _dataContext;

    public GetRcmBillingByBillIdHandler(PADDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public Task<PADRCMBilling> Handle(GetRcmBillingByBillId request, CancellationToken cancellationToken)
    {
        return _dataContext.PADRCMBilling.AsNoTracking()
            .FirstOrDefaultAsync(x => string.Equals(x.BillId.ToUpper(), request.BillId.ToUpper()), cancellationToken);
    }
}