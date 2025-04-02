using MediatR;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Signify.DEE.Svc.Core.Messages.Commands;

public class CreateRcmBilling : IRequest<Unit>
{
    public DEEBilling RcmBilling { get; set; }
       
}

public class CreateRcmBillingHandler(DataContext context) : IRequestHandler<CreateRcmBilling, Unit>
{
    [Trace]
    public async Task<Unit> Handle(CreateRcmBilling request, CancellationToken cancellationToken)
    {
        var entity = await context.DEEBilling
            .AsNoTracking()
            .FirstOrDefaultAsync(each =>
                each.BillId.Equals(request.RcmBilling.BillId), cancellationToken);

        if (entity == null)
        {
            await context.DEEBilling.AddAsync(request.RcmBilling, cancellationToken);
        }
        else {
            context.DEEBilling.Update(request.RcmBilling);
        }

        await context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}