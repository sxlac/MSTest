using MediatR;
using Microsoft.EntityFrameworkCore;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Messages.Queries;

public class GetExamByLocalId : IRequest<Exam>
{
    public string LocalId { get; set; }
}

public class GetExamByLocalIdHandler(DataContext context) : IRequestHandler<GetExamByLocalId, Exam>
{
    [Trace]
    public async Task<Exam> Handle(GetExamByLocalId request, CancellationToken cancellationToken)
    {
        var entity = await context.Exams.Include(e => e.EvaluationObjective)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ExamLocalId == request.LocalId, cancellationToken);

        return entity;
    }
}