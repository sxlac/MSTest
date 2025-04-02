using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Messages.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Messages.Commands;

/// <summary>
/// Command to add details about why a DEE exam was not performed to database
/// </summary>
public class AddDeeNotPerformed : IRequest<DeeNotPerformed>
{
    public ExamModel ExamModel { get; set; }
    public NotPerformedModel NotPerformedModel { get; set; }

}

public class AddDeeNotPerformedHandler(ILogger<AddDeeNotPerformedHandler> logger, IMapper mapper, DataContext context)
    : IRequestHandler<AddDeeNotPerformed, DeeNotPerformed>
{
    public async Task<DeeNotPerformed> Handle(AddDeeNotPerformed request, CancellationToken cancellationToken)
    {
        var reason = await context.DeeNotPerformed.AsNoTracking().FirstOrDefaultAsync(e => e.ExamId == request.ExamModel.ExamId, cancellationToken);

        if (reason == null)
        {
            var entity = mapper.Map<DeeNotPerformed>(request.NotPerformedModel);
            entity.ExamId = request.ExamModel.ExamId;

            entity = (await context.DeeNotPerformed.AddAsync(entity, cancellationToken)).Entity;

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully added a new DeeNotPerformed record for ExamId={ExamId}; new DeeNotPerformedId={DeeNotPerformedId}",
                entity.ExamId, entity.DeeNotPerformedId);

            return entity;
        }

        logger.LogInformation("DeeNotPerformed record already exists for ExamId={ExamId}; new DeeNotPerformedId={DeeNotPerformedId}",
            reason.ExamId, reason.DeeNotPerformedId);

        return reason;
    }
}