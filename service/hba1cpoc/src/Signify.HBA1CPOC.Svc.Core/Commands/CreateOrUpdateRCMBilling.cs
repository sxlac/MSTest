using MediatR;
using NewRelic.Api.Agent;
using Signify.HBA1CPOC.Svc.Core.Data;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.HBA1CPOC.Svc.Core.Commands
{
    public class CreateOrUpdateRCMBilling : IRequest<Unit>
    {
        public HBA1CPOCRCMBilling RcmBilling { get; set; }
    }

    public class CreateRCMBillingHandler : IRequestHandler<CreateOrUpdateRCMBilling, Unit>
    {
        private readonly Hba1CpocDataContext _context;

        public CreateRCMBillingHandler(Hba1CpocDataContext context)
        {
            _context = context;
        }

        [Trace]
        public async Task<Unit> Handle(CreateOrUpdateRCMBilling request, CancellationToken cancellationToken)
        {
            if (request.RcmBilling.Id == 0)
            {
                await _context.HBA1CPOCRCMBilling.AddAsync(request.RcmBilling, cancellationToken);
            }
            else
            {
                _context.HBA1CPOCRCMBilling.Update(request.RcmBilling);
            }
            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
