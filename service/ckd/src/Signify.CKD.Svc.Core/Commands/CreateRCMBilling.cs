using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NewRelic.Api.Agent;
using Signify.CKD.Svc.Core.Data;
using Signify.CKD.Svc.Core.Data.Entities;

namespace Signify.CKD.Svc.Core.Commands
{
    public class CreateRCMBilling : IRequest<Unit>
    {
        public CKDRCMBilling RcmBilling { get; set; }
    }

    public class CreateRCMBillingHandler : IRequestHandler<CreateRCMBilling, Unit>
    {
        private readonly CKDDataContext _context;

        public CreateRCMBillingHandler(CKDDataContext context)
        {
            _context = context;
        }

        [Trace]
        public async Task<Unit> Handle(CreateRCMBilling request, CancellationToken cancellationToken)
        {
            await _context.CKDRCMBilling.AddAsync(request.RcmBilling, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
