using MediatR;
using Microsoft.EntityFrameworkCore;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Messages.Queries;

public class GetGradableStatus(int examId) : IRequest<bool?>
{
    public int ExamId { get; set; } = examId;
}

public class GetGradableStatusHandler(DataContext context) : IRequestHandler<GetGradableStatus, bool?>
{
    [Trace]
    public async Task<bool?> Handle(GetGradableStatus request, CancellationToken cancellationToken)
    {
        var examGradableStatus = await context.ExamStatuses.FirstOrDefaultAsync
        (s => s.ExamId == request.ExamId && (s.ExamStatusCodeId == ExamStatusCode.Gradable.ExamStatusCodeId || s.ExamStatusCodeId == ExamStatusCode.NotGradable.ExamStatusCodeId),
            cancellationToken: cancellationToken);

        if (examGradableStatus == null) return null;

        var response = examGradableStatus?.ExamStatusCodeId == ExamStatusCode.Gradable.ExamStatusCodeId;
        return response;
    }
}