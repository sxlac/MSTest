using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Messages.Queries;

public class GetExamImages : IRequest<IEnumerable<ExamImage>>
{
    public int ExamId { get; set; }
}

public class GetExamImagesHandler(ILogger<GetExamImagesHandler> log, DataContext context)
    : IRequestHandler<GetExamImages, IEnumerable<ExamImage>>
{
    [Trace]
    public async Task<IEnumerable<ExamImage>> Handle(GetExamImages request, CancellationToken cancellationToken)
    {
        using var scope = log.BeginScope("ExamId={ExamId}", request.ExamId);

        return await context.ExamImages
            .AsNoTracking()
            .Where(x => x.ExamId == request.ExamId)
            .ToListAsync(cancellationToken);
    }
}