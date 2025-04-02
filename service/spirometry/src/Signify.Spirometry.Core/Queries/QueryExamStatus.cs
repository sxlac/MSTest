using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.Queries
{
    public class QueryExamStatus : IRequest<ExamStatus>
    {
        public int SpirometryExamId { get; init; }
        public StatusCode StatusCode { get; init; }
    }

    public class QueryExamStatusHandler : IRequestHandler<QueryExamStatus, ExamStatus>
    {
        private readonly SpirometryDataContext _dataContext;

        public QueryExamStatusHandler(SpirometryDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public Task<ExamStatus> Handle(QueryExamStatus request, CancellationToken cancellationToken)
        {
            return _dataContext.ExamStatuses
                .AsNoTracking()
                .FirstOrDefaultAsync(s =>
                        s.SpirometryExamId == request.SpirometryExamId &&
                        s.StatusCodeId == request.StatusCode.StatusCodeId,
                    cancellationToken);
        }
    }
}
