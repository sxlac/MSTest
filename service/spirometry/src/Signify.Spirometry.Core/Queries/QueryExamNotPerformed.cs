using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.Queries
{
    /// <summary>
    /// Query to search for <see cref="ExamNotPerformed"/> by a given EvaluationId, if one exists
    /// </summary>
    public class QueryExamNotPerformed : IRequest<ExamNotPerformed>
    {
        public long EvaluationId { get; }

        public QueryExamNotPerformed(long evaluationId)
        {
            EvaluationId = evaluationId;
        }
    }

    public class QueryExamNotPerformedHandler : IRequestHandler<QueryExamNotPerformed, ExamNotPerformed>
    {
        private readonly SpirometryDataContext _dataContext;

        public QueryExamNotPerformedHandler(SpirometryDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<ExamNotPerformed> Handle(QueryExamNotPerformed request, CancellationToken cancellationToken)
        {
            return await _dataContext.ExamNotPerformeds
                .Include(npr => npr.NotPerformedReason)
                .AsNoTracking()
                .FirstOrDefaultAsync(each => each.SpirometryExam.EvaluationId == request.EvaluationId,
                    cancellationToken);
        }
    }
}