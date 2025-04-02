using MediatR;
using Microsoft.EntityFrameworkCore;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Messages.Queries;

public class GetIncompleteExamStatusCodeId(int examId) : IRequest<int?>
{
    public int ExamId { get; set; } = examId;
}

public class GetIncompleteExamStatusCodeIdHandler(DataContext context)
    : IRequestHandler<GetIncompleteExamStatusCodeId, int?>
{
    [Trace]
    public async Task<int?> Handle(GetIncompleteExamStatusCodeId request, CancellationToken cancellationToken)
    {
        var examIncompleteStatus = await context.ExamStatuses.FirstOrDefaultAsync(s => s.ExamId == request.ExamId && s.ExamStatusCodeId == ExamStatusCode.Incomplete.ExamStatusCodeId, cancellationToken: cancellationToken);
        var response = examIncompleteStatus?.ExamStatusCodeId;
        return response;
    }
}