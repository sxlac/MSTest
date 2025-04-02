using MediatR;
using NewRelic.Api.Agent;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.Commands
{
    /// <summary>
    /// Command to add a <see cref="ClarificationFlag"/> to the database
    /// </summary>
    public class AddClarificationFlag : IRequest<ClarificationFlag>
    {
        public ClarificationFlag Flag { get; }

        public AddClarificationFlag(ClarificationFlag flag)
        {
            Flag = flag;
        }
    }

    public class AddClarificationFlagHandler : IRequestHandler<AddClarificationFlag, ClarificationFlag>
    {
        private readonly SpirometryDataContext _context;

        public AddClarificationFlagHandler(SpirometryDataContext context)
        {
            _context = context;
        }

        [Transaction]
        public async Task<ClarificationFlag> Handle(AddClarificationFlag request, CancellationToken cancellationToken)
        {
            var entity = (await _context.ClarificationFlags.AddAsync(request.Flag, cancellationToken)).Entity;

            await _context.SaveChangesAsync(cancellationToken);

            return entity;
        }
    }
}
