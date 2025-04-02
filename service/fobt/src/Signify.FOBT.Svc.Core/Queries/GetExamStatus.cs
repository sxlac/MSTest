using MediatR;
using Microsoft.EntityFrameworkCore;
using NewRelic.Api.Agent;
using Signify.FOBT.Svc.Core.Data;
using System.Threading;
using System.Threading.Tasks;
using Signify.FOBT.Svc.Core.Data.Entities;

namespace Signify.FOBT.Svc.Core.Queries;

public class GetExamStatus : IRequest<FOBTStatus>
{
    public int ExamId { get; }

    public int ExamStatusCodeId { get; }

    public GetExamStatus(int examId, int examStatusCodeId)
    {
        ExamId = examId;
        ExamStatusCodeId = examStatusCodeId;
    }
}

public class GetExamStatusHandler : IRequestHandler<GetExamStatus, FOBTStatus>
{
    private readonly FOBTDataContext _context;

    public GetExamStatusHandler(FOBTDataContext context)
    {
        _context = context;
    }

    [Trace]
    public async Task<FOBTStatus> Handle(GetExamStatus request, CancellationToken cancellationToken)
    {
        var examStatus = await _context.FOBTStatus.AsNoTracking()
            .FirstOrDefaultAsync(each => each.FOBTId == request.ExamId && each.FOBTStatusCodeId == request.ExamStatusCodeId, cancellationToken);

        return examStatus;
    }
}