using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Signify.HBA1CPOC.Svc.Core.Data;

namespace Signify.HBA1CPOC.Svc.Core.Queries;

public class QueryExamStatuses : IRequest<List<HBA1CPOCStatus>>
{
    public int ExamId { get; init; }
}

public class QueryExamStatusesHandler : IRequestHandler<QueryExamStatuses, List<HBA1CPOCStatus>>
{
    private readonly Hba1CpocDataContext _context;

    public QueryExamStatusesHandler(Hba1CpocDataContext context)
    {
        _context = context;
    }

    public Task<List<HBA1CPOCStatus>> Handle(QueryExamStatuses request, CancellationToken cancellationToken)
    {
        return _context.HBA1CPOCStatus
            .AsNoTracking()
            .Include(s => s.HBA1CPOC)
            .Include(s => s.HBA1CPOCStatusCode)
            .Where(each => each.HBA1CPOC.HBA1CPOCId == request.ExamId)
            .ToListAsync(cancellationToken);
    }
}
