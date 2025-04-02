using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;

namespace Signify.Spirometry.Core.Queries;

public class QueryCdiEventForPayment : IRequest<CdiEventForPayment>
{
    public Guid RequestId { get; set; }

    public QueryCdiEventForPayment(Guid requestId)
    {
        RequestId = requestId;
    }
}

public class QueryCdiEventForPaymentHandler : IRequestHandler<QueryCdiEventForPayment, CdiEventForPayment>
{
    private readonly SpirometryDataContext _dataContext;

    public QueryCdiEventForPaymentHandler(SpirometryDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public async Task<CdiEventForPayment> Handle(QueryCdiEventForPayment request, CancellationToken cancellationToken)
    {
        var entity = await _dataContext.CdiEventForPayments
            .AsNoTracking()
            .FirstOrDefaultAsync(each => each.RequestId == request.RequestId, cancellationToken);

        return entity;
    }
}