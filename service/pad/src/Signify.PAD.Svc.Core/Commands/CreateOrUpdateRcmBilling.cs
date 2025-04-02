using MediatR;
using NewRelic.Api.Agent;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.PAD.Svc.Core.Commands;

public class CreateOrUpdateRcmBilling : IRequest<Unit>
{
    public PADRCMBilling RcmBilling { get; set; }
}

public class CreateOrUpdateRcmBillingHandler : IRequestHandler<CreateOrUpdateRcmBilling, Unit>
{
    private readonly PADDataContext _context;

    public CreateOrUpdateRcmBillingHandler(PADDataContext context)
    {
        _context = context;
    }

    [Trace]
    public async Task<Unit> Handle(CreateOrUpdateRcmBilling request, CancellationToken cancellationToken)
    {
        if (request.RcmBilling.Id == 0)
        {
            await _context.PADRCMBilling.AddAsync(request.RcmBilling, cancellationToken);
        }
        else
        {
            _context.PADRCMBilling.Update(request.RcmBilling);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}