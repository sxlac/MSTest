using MediatR;
using Microsoft.EntityFrameworkCore;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Messages.Queries;

public class GetExamImageByLocalId : IRequest<ExamImage>
{
    public string LocalId { get; set; }
}

public class GetExamImageByLocalIdHandler(DataContext context) : IRequestHandler<GetExamImageByLocalId, ExamImage>
{
    [Trace]
    public async Task<ExamImage> Handle(GetExamImageByLocalId request, CancellationToken cancellationToken)
    {
        return await context.ExamImages
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ImageLocalId == request.LocalId, cancellationToken);
    }
}