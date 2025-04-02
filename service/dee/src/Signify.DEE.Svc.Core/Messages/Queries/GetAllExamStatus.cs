using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.Data;

namespace Signify.DEE.Svc.Core.Messages.Queries;

public class GetAllExamStatus(int examId) : IRequest<List<int>>
{
    public int ExamId { get; set; } = examId;
}

public class GetAllExamStatusHandler(DataContext context) : IRequestHandler<GetAllExamStatus, List<int>>
{
    [Transaction]
    public async Task<List<int>> Handle(GetAllExamStatus request, CancellationToken cancellationToken)
    {
        var allStatus = await context.ExamStatuses.AsNoTracking()
            .Where(s => s.ExamId == request.ExamId).Select(s=>s.ExamStatusCodeId)
            .ToListAsync(cancellationToken);

        return allStatus;
    }
}