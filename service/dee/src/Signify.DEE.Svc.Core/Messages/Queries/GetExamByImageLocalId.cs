using MediatR;
using Microsoft.EntityFrameworkCore;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Messages.Queries;

public class GetExamByImageLocalId : IRequest<Exam>
{
    public string ImageLocalId { get; set; }
}

public class GetExamByImageLocalIdHandler(DataContext context) : IRequestHandler<GetExamByImageLocalId, Exam>
{
    [Trace]
    public async Task<Exam> Handle(GetExamByImageLocalId request, CancellationToken cancellationToken)
    {
        var exam = await context.ExamImages.Where(img => img.ImageLocalId == request.ImageLocalId)
            .Join(context.Exams,
            img => img.ExamId,
            ex => ex.ExamId,
            (img, ex) => ex)
            .FirstOrDefaultAsync(cancellationToken);

        return exam;
    }
}