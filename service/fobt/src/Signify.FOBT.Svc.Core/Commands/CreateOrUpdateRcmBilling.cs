using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NewRelic.Api.Agent;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Data.Entities;

namespace Signify.FOBT.Svc.Core.Commands;

public class CreateOrUpdateRcmBilling : IRequest<Unit>
{
    public FOBTBilling RcmBilling { get; set; }
}

public class CreateOrUpdateRcmBillingHandler : IRequestHandler<CreateOrUpdateRcmBilling, Unit>
{
    private readonly FOBTDataContext _context;

    public CreateOrUpdateRcmBillingHandler(FOBTDataContext context)
    {
        _context = context;
    }

    [Trace]
    public async Task<Unit> Handle(CreateOrUpdateRcmBilling request, CancellationToken cancellationToken)
    {
        if (request.RcmBilling.Id == 0)
        {
            await _context.FOBTBilling.AddAsync(request.RcmBilling, cancellationToken);
        }
        else
        {
            _context.FOBTBilling.Update(request.RcmBilling);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}