using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NewRelic.Api.Agent;
using Signify.A1C.Svc.Core.Data;
using Signify.A1C.Svc.Core.Data.Entities;

namespace Signify.A1C.Svc.Core.Commands
{
    public class CreateA1CStatus : IRequest<A1CStatus>
    {
        public int A1CId { get; set; }
        public A1CStatusCode StatusCode { get; set; }
    }

    public class CreateA1CStatusHandler : IRequestHandler<CreateA1CStatus, A1CStatus>
    {
        private readonly A1CDataContext _context;

        public CreateA1CStatusHandler(A1CDataContext context)
        {
            _context = context;
        }

        [Trace]
        public async Task<A1CStatus> Handle(CreateA1CStatus request, CancellationToken cancellationToken)
        {

            var a1CStatus = new A1CStatus 
            {
                A1CId = request.A1CId,
                A1CStatusCode = A1CStatusCode.GetA1CStatusCode(request.StatusCode.A1CStatusCodeId),
                CreatedDateTime = DateTimeOffset.Now
            };

            //attach A1CStatusCode, A1C to database context
            _context.A1CStatusCode.Attach(a1CStatus.A1CStatusCode);

            var logStatus = await _context.A1CStatus.AddAsync(a1CStatus, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return logStatus.Entity;

        }

    }
}
