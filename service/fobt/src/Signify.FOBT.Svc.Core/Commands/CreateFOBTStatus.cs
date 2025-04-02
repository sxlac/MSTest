using MediatR;
using NewRelic.Api.Agent;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Data.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.FOBT.Svc.Core.Commands;

public sealed class CreateFOBTStatus : IRequest<FOBTStatus>
{
    public FOBTStatusCode StatusCode { get; set; }
    public Data.Entities.FOBT FOBT { get; set; }

}

public class CreateFOBTStatusHandler : IRequestHandler<CreateFOBTStatus, FOBTStatus>
{
    private readonly FOBTDataContext _context;

    public CreateFOBTStatusHandler(FOBTDataContext context)
    {
        _context = context;
    }

    [Trace]
    public async Task<FOBTStatus> Handle(CreateFOBTStatus request, CancellationToken cancellationToken)
    {
        var fobtStatus = new FOBTStatus
        {
            FOBTStatusCodeId = FOBTStatusCode.GetFOBTStatusCode(request.StatusCode.FOBTStatusCodeId).FOBTStatusCodeId,
            FOBTId = request.FOBT.FOBTId,
            CreatedDateTime = DateTimeOffset.UtcNow
        };
        var entity = (await _context.FOBTStatus.AddAsync(fobtStatus, cancellationToken)).Entity;

        await _context.SaveChangesAsync(cancellationToken);

        return entity;
    }
}