using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;

namespace Signify.Spirometry.Core.Queries;

/// <summary>
/// Query to see if a <see cref="BillRequestSent"/> record exists for a given billID
/// </summary>
public class QueryBillRequestSentByBillId : IRequest<BillRequestSent>
{
    public Guid BillId { get; }

    public QueryBillRequestSentByBillId(Guid billId)
    {
        BillId = billId;
    }
}

public class QueryBillRequestSentByBillIdHandler : IRequestHandler<QueryBillRequestSentByBillId, BillRequestSent>
{
    private readonly SpirometryDataContext _dataContext;

    public QueryBillRequestSentByBillIdHandler(SpirometryDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public Task<BillRequestSent> Handle(QueryBillRequestSentByBillId request, CancellationToken cancellationToken)
    {
        return _dataContext.BillRequestSents.AsNoTracking()
            .FirstOrDefaultAsync(x => Equals(x.BillId, request.BillId), cancellationToken);
    }
}