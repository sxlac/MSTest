using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.DEE.Messages;
using Signify.DEE.Svc.Core.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Messages.Queries;

public class GetResultReceivedData(int examId) : IRequest<Result>
{
    public int ExamId { get; } = examId;
}

public class GetResultReceivedDataHandler(
    ILogger<GetResultReceivedDataHandler> logger,
    DataContext context,
    IMapper mapper)
    : IRequestHandler<GetResultReceivedData, Result>
{
    [Trace]
    public async Task<Result> Handle(GetResultReceivedData request, CancellationToken cancellationToken)
    {
        var exam = await context.Exams
            .Include(e => e.ExamImages)
            .Include(e => e.ExamLateralityGrades)
            .ThenInclude(e => e.NonGradableReasons)
            .Include(e => e.ExamStatuses)
            .Include(e => e.ExamResults)
            .ThenInclude(examResult => examResult.ExamFindings)
            .Include(e => e.ExamResults)
            .ThenInclude(examResult => examResult.ExamDiagnoses)
            .AsNoTracking()
            .SingleAsync(e => e.ExamId == request.ExamId, cancellationToken);

        if (exam != null)
            return mapper.Map<Result>(exam);

        logger.LogWarning("Failed to find an exam with ExamId={ExamId}", request.ExamId);
        return null;
    }
}