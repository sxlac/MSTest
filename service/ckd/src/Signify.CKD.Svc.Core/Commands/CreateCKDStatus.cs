using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NewRelic.Api.Agent;
using Signify.CKD.Svc.Core.Data;
using Signify.CKD.Svc.Core.Data.Entities;

namespace Signify.CKD.Svc.Core.Commands
{
    public class CreateCKDStatus : IRequest<CKDStatus>
    {
        public int StatusCodeId { get; set; }
        public int CKDId { get; set; }

    }

    public class CreateCKDStatusHandler : IRequestHandler<CreateCKDStatus, CKDStatus>
    {
        private readonly CKDDataContext _context;

        public CreateCKDStatusHandler(CKDDataContext context)
        {
            _context = context;
        }

        [Trace]
        public async Task<CKDStatus> Handle(CreateCKDStatus request, CancellationToken cancellationToken)
        {
            var CKDStatus = new CKDStatus()
            {
                CKDId = request.CKDId,
                CreatedDateTime = DateTimeOffset.Now,
                CKDStatusCodeId = request.StatusCodeId
            };
            var logStatus = await _context.CKDStatus.AddAsync(CKDStatus, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return logStatus.Entity;
        }

    }
}
