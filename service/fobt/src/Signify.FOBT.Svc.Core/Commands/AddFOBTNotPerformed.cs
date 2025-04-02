using System.Diagnostics.CodeAnalysis;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.FOBT.Svc.Core.Commands;

/// <summary>
/// Command to add details about why a FOBT exam was not performed to database
/// </summary>
[ExcludeFromCodeCoverage]
public class AddFOBTNotPerformed : IRequest<FOBTNotPerformed>
{
    public Data.Entities.FOBT FOBT { get; set; }
    public NotPerformedReason NotPerformedReason { get; set; }
    public string Notes { get; set; }
}

public class AddFOBTNotPerformedHandler : IRequestHandler<AddFOBTNotPerformed, FOBTNotPerformed>
{
    private readonly ILogger _logger;
    private readonly FOBTDataContext _context;
    private readonly IMapper _mapper;

    public AddFOBTNotPerformedHandler(ILogger<AddFOBTNotPerformedHandler> logger,
        FOBTDataContext context,
        IMapper mapper)
    {
        _logger = logger;
        _context = context;
        _mapper = mapper;
    }

    public async Task<FOBTNotPerformed> Handle(AddFOBTNotPerformed request, CancellationToken cancellationToken)
    {
        var reason = await _context.FOBTNotPerformed.AsNoTracking().FirstOrDefaultAsync(e => e.FOBTId == request.FOBT.FOBTId, cancellationToken: cancellationToken);

        if (reason == null)
        {
            var entity = _mapper.Map<FOBTNotPerformed>(request.NotPerformedReason);
            entity.FOBTId = request.FOBT.FOBTId;
            entity.Notes = request.Notes;

            entity = (await _context.FOBTNotPerformed.AddAsync(entity, cancellationToken)).Entity;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully added a new FOBTNotPerformed record for FOBTId={FobtId}; new FOBTNotPerformedId={FobtNotPerformedId}",
                entity.FOBTId, entity.FOBTNotPerformedId);

            return entity;
        }

        _logger.LogInformation("FOBTNotPerformed record already exists for FOBTId={FobtId}; new FOBTNotPerformedId={FobtNotPerformedId}",
            reason.FOBTId, reason.FOBTNotPerformedId);

        return reason;
          
    }
}