using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NewRelic.Api.Agent;
using Signify.PAD.Svc.Core.Data;

namespace Signify.PAD.Svc.Core.Commands;

public class CreateNotPerformed : IRequest<Unit>
{
    public Data.Entities.NotPerformed NotPerformedRec { get; set; }
}

public class CreateNotPerformedHandler : IRequestHandler<CreateNotPerformed, Unit>
{
    private readonly PADDataContext _context;

    public CreateNotPerformedHandler(PADDataContext context)
    {
        _context = context;
    }

    [Trace]
    public async Task<Unit> Handle(CreateNotPerformed request, CancellationToken cancellationToken)
    {
        var notPerformed = new Data.Entities.NotPerformed()
        {
            AnswerId = request.NotPerformedRec.AnswerId,
            Notes = request.NotPerformedRec.Notes,
            PADId = request.NotPerformedRec.PADId
        };
            
        _ = await _context.NotPerformed.AddAsync(notPerformed, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}