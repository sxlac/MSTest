using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.uACR.Core.Data;
using Signify.uACR.Core.Data.Entities;

namespace Signify.uACR.Core.Commands;

/// <summary>
/// Command to Create a <see cref="Data.Entities.LabResult"/> entity in db
/// </summary>
public class AddLabResult(LabResult labResult) : IRequest<LabResult>
{
    public LabResult LabResult { get; } = labResult;
}

public class AddLabResultHandler(
    ILogger<AddLabResultHandler> logger,
    DataContext dataContext)
    : IRequestHandler<AddLabResult, LabResult>
{
    public async Task<LabResult> Handle(AddLabResult request, CancellationToken cancellationToken)
    {
        var entity = (await dataContext.LabResults.AddAsync(request.LabResult, cancellationToken)
            .ConfigureAwait(false)).Entity;

        await dataContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Successfully inserted a new LabResult record. New LabResultId={LabResultId}", entity.LabResultId);

        return entity;
    }
}