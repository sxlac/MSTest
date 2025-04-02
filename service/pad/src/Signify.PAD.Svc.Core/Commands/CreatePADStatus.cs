using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NewRelic.Api.Agent;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Infrastructure;

namespace Signify.PAD.Svc.Core.Commands;

public class CreatePadStatus : IRequest<PADStatus>
{
    public PADStatusCode StatusCode { get; set; }
    public int PadId { get; set; }
}

public class CreatePadStatusHandler : IRequestHandler<CreatePadStatus, PADStatus>
{
    private readonly PADDataContext _context;
    private readonly IApplicationTime _applicationTime;

    public CreatePadStatusHandler(PADDataContext context, IApplicationTime applicationTime)
    {
        _context = context;
        _applicationTime = applicationTime;
    }

    [Trace]
    public async Task<PADStatus> Handle(CreatePadStatus request, CancellationToken cancellationToken)
    {
        var padStatus = new PADStatus()
        {
            PADStatusCodeId = request.StatusCode.PADStatusCodeId,
            PADId = request.PadId,
            CreatedDateTime = _applicationTime.UtcNow()
        };
        var entity = (await _context.PADStatus.AddAsync(padStatus, cancellationToken)).Entity;
        await _context.SaveChangesAsync(cancellationToken);
            
        return entity;
    } 
}