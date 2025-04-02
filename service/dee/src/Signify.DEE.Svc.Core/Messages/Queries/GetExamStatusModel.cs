using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Messages.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Messages.Queries;

public class GetExamStatusModel(int examId, int examStatusCodeId) : IRequest<ExamStatusModel>
{
    public int ExamId { get; } = examId;

    public int ExamStatusCodeId { get; } = examStatusCodeId;
}

public class GetReceivedDateHandler(DataContext context, IMapper mapper)
    : IRequestHandler<GetExamStatusModel, ExamStatusModel>
{
    [Trace]
    public async Task<ExamStatusModel> Handle(GetExamStatusModel request, CancellationToken cancellationToken)
    {
        var examStatus = await context.ExamStatuses.AsNoTracking()
            .FirstOrDefaultAsync(each => each.ExamId == request.ExamId && each.ExamStatusCodeId == request.ExamStatusCodeId, cancellationToken);

        return examStatus != null ? mapper.Map<ExamStatusModel>(examStatus) : null;
    }
}