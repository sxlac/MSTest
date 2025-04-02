using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.Queries
{
    /// <summary>
    /// Query to search for <see cref="SpirometryExamResult"/> by a given EvaluationId, if one exists
    /// </summary>
    public class QueryExamResults : IRequest<SpirometryExamResult>
    {
        public long EvaluationId { get; }

        public QueryExamResults(long evaluationId)
        {
            EvaluationId = evaluationId;
        }
    }

    public class QueryExamResultsHandler : IRequestHandler<QueryExamResults, SpirometryExamResult>
    {
        private readonly SpirometryDataContext _dataContext;

        public QueryExamResultsHandler(SpirometryDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<SpirometryExamResult> Handle(QueryExamResults request, CancellationToken cancellationToken)
        {
            return await _dataContext.SpirometryExamResults
                .AsNoTracking()
                .FirstOrDefaultAsync(each => each.SpirometryExam.EvaluationId == request.EvaluationId, cancellationToken);
        }
    }
}