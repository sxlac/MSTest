using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.Queries
{
    /// <summary>
    /// Query to see if a <see cref="BillRequestSent"/> record exists for a given evaluation
    /// </summary>
    public class QueryBillRequestSent : IRequest<QueryBillRequestSentResult>
    {
        public long EvaluationId { get; }

        public QueryBillRequestSent(long evaluationId)
        {
            EvaluationId = evaluationId;
        }
    }

    /// <summary>
    /// Result of a <see cref="QueryBillRequestSent"/>
    /// </summary>
    public class QueryBillRequestSentResult
    {
        /// <summary>
        /// The <see cref="BillRequestSent"/> entity returned by the <see cref="QueryBillRequestSent"/>, if one exists
        /// </summary>
        public BillRequestSent Entity { get; }

        public QueryBillRequestSentResult(BillRequestSent billRequestSent)
        {
            Entity = billRequestSent;
        }
    }

    public class QueryBillRequestSentHandler : IRequestHandler<QueryBillRequestSent, QueryBillRequestSentResult>
    {
        private readonly SpirometryDataContext _dataContext;

        public QueryBillRequestSentHandler(SpirometryDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<QueryBillRequestSentResult> Handle(QueryBillRequestSent request, CancellationToken cancellationToken)
        {
            var entity = await _dataContext.BillRequestSents
                .AsNoTracking()
                .FirstOrDefaultAsync(each => each.SpirometryExam.EvaluationId == request.EvaluationId, cancellationToken);

            return new QueryBillRequestSentResult(entity);
        }
    }
}
