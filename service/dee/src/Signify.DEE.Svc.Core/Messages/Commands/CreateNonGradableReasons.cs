using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Signify.DEE.Svc.Core.Data.Entities;
using System.Linq;
using Signify.DEE.Svc.Core.Data;
using NewRelic.Api.Agent;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Signify.DEE.Svc.Core.Messages.Commands;

public class CreateNonGradableReasons : IRequest<Unit>
{
    public int ExamLateralityGradeId { get; set; }
    public IEnumerable<string> Reasons { get; set; }
}

public class CreateNonGradableReasonsHandler(ILogger<CreateNonGradableReasonsHandler> logger, DataContext context)
    : IRequestHandler<CreateNonGradableReasons, Unit>
{
    [Trace]
    public async Task<Unit> Handle(CreateNonGradableReasons request, CancellationToken cancellationToken)
    {
        var exists = await context.NonGradableReason.
            AsNoTracking().
            AnyAsync(i => i.ExamLateralityGradeId == request.ExamLateralityGradeId, cancellationToken);

        if (exists)
        {
            logger.LogInformation("Non-gradable reasons already exist for ExamLateralityGradeId={ExamLateralityGradeId}", request.ExamLateralityGradeId);
        }
        else
        {
            var nonGradableReasons = request.Reasons.Select(
                reason => new NonGradableReason
                {
                    ExamLateralityGradeId = request.ExamLateralityGradeId,
                    Reason = reason
                }
            );

            logger.LogInformation("Adding new non-gradable reasons, for ExamLateralityGradeId={ExamLateralityGradeId}",
                request.ExamLateralityGradeId);

            await context.NonGradableReason.AddRangeAsync(nonGradableReasons, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
        return Unit.Value;
    }
}