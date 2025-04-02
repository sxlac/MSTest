using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Data;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace Signify.uACR.Core.Queries;

/// <summary>
/// Query to see if a <see cref="BillRequest"/> record exists for a given evaluation
/// </summary>
public class QueryBillRequests : IRequest<QueryBillRequestsResult>
{
    public long EvaluationId { get; }
    public Guid BillId { get; }

    public string BillingProductCode { get; set; }
    
    public QueryBillRequests(long evaluationId, string billingProductCode)
    {
        EvaluationId = evaluationId;
        BillingProductCode = billingProductCode;
    }

    public QueryBillRequests(Guid billId)
    {
        BillId = billId;
    }
}

/// <summary>
/// Result of a <see cref="QueryBillRequests"/>
/// </summary>
public class QueryBillRequestsResult(BillRequest billRequest)
{
    /// <summary>
    /// The <see cref="BillRequest"/> entity returned by the <see cref="QueryBillRequests"/>, if one exists
    /// </summary>
    public BillRequest Entity { get; } = billRequest;
}

public class QueryBillRequestsHandler(DataContext dataContext)
    : IRequestHandler<QueryBillRequests, QueryBillRequestsResult>
{
    public async Task<QueryBillRequestsResult> Handle(QueryBillRequests request, CancellationToken cancellationToken)
    {
        var entity = await dataContext.BillRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(each =>
                request.EvaluationId > 0
                    ? each.Exam.EvaluationId == request.EvaluationId && each.BillingProductCode == request.BillingProductCode
                    : each.BillId.Equals(request.BillId), cancellationToken);

        return new QueryBillRequestsResult(entity);
    }
}