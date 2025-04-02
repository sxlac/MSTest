using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Data.Entities;

namespace Signify.FOBT.Svc.Core.Queries;

public class QueryExamStatuses : IRequest<List<FOBTStatus>>
{
    public int ExamId { get; init; }
}

public class QueryExamStatusesHandler : IRequestHandler<QueryExamStatuses, List<FOBTStatus>>
{
    private readonly FOBTDataContext _context;

    public QueryExamStatusesHandler(FOBTDataContext context)
    {
        _context = context;
    }

    public Task<List<FOBTStatus>> Handle(QueryExamStatuses request, CancellationToken cancellationToken)
        => _context.FOBTStatus
            .AsNoTracking()
            .Where(each => each.FOBTId == request.ExamId)
            .ToListAsync(cancellationToken);
}