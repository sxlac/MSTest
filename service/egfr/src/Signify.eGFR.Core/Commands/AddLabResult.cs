using MediatR;
using Microsoft.Extensions.Logging;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.eGFR.Core.Commands;

/// <summary>
/// Command to Create a <see cref="LabResult"/> entity in db
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
    private readonly ILogger _logger = logger;

    public async Task<LabResult> Handle(AddLabResult request, CancellationToken cancellationToken)
    {
        var entity = (await dataContext.LabResults.AddAsync(request.LabResult, cancellationToken)
            .ConfigureAwait(false)).Entity;

        await dataContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Successfully inserted a new LabResult record. New LabResultId={LabResultId}", entity.LabResultId);

        return entity;
    }
}