using System;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.eGFR.Core.Queries;

/// <summary>
/// Query to see if a <see cref="BillRequestSent"/> record exists for a given evaluation
/// </summary>
public class QueryBillRequestSent : IRequest<QueryBillRequestSentResult>
{
    public long EvaluationId { get; }
    public Guid BillId { get; }

    public string BillingProductCode { get; set; }
    
    public QueryBillRequestSent(long evaluationId, string billingProductCode)
    {
        EvaluationId = evaluationId;
        BillingProductCode = billingProductCode;
    }

    public QueryBillRequestSent(Guid billId)
    {
        BillId = billId;
    }
}

/// <summary>
/// Result of a <see cref="QueryBillRequestSent"/>
/// </summary>
public class QueryBillRequestSentResult(BillRequestSent billRequestSent)
{
    /// <summary>
    /// The <see cref="BillRequestSent"/> entity returned by the <see cref="QueryBillRequestSent"/>, if one exists
    /// </summary>
    public BillRequestSent Entity { get; } = billRequestSent;
}

public class QueryBillRequestSentHandler(DataContext dataContext)
    : IRequestHandler<QueryBillRequestSent, QueryBillRequestSentResult>
{
    public async Task<QueryBillRequestSentResult> Handle(QueryBillRequestSent request, CancellationToken cancellationToken)
    {
        var entity = await dataContext.BillRequestSents
            .AsNoTracking()
            .FirstOrDefaultAsync(each =>
                request.EvaluationId > 0
                    ? each.Exam.EvaluationId == request.EvaluationId && each.BillingProductCode == request.BillingProductCode
                    : each.BillId.Equals(request.BillId), cancellationToken);

        return new QueryBillRequestSentResult(entity);
    }
}