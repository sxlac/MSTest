using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Messages.Queries;

public class GetNotPerformedExamByExamId : IRequest<DeeNotPerformed>
{
    public long ExamId { get; set; }

    /// <summary>
    /// Whether to include statuses with the entity
    /// </summary>
    public bool IncludeStatuses { get; init; }
}

public class GetNotPerformedExamByExamIdHandler(ILogger<GetNotPerformedExamByExamIdHandler> log, DataContext context)
    : IRequestHandler<GetNotPerformedExamByExamId, DeeNotPerformed>
{
    [Trace]
    public Task<DeeNotPerformed> Handle(GetNotPerformedExamByExamId request, CancellationToken cancellationToken)
    {
        log.LogDebug("{request} -- ExamNotPerformed lookup", request);
        var queryable = context.DeeNotPerformed.AsNoTracking();       
        return queryable.FirstOrDefaultAsync(s => s.ExamId == request.ExamId, cancellationToken);
    }
}