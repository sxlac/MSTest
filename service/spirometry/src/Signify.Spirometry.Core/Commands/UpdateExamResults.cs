using MediatR;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.Commands
{
    public class UpdateExamResults : IRequest
    {
        public SpirometryExamResult Results { get; }

        public UpdateExamResults(SpirometryExamResult results)
        {
            Results = results;
        }
    }

    public class UpdateExamResultsHandler : IRequestHandler<UpdateExamResults>
    {
        private readonly SpirometryDataContext _context;

        public UpdateExamResultsHandler(SpirometryDataContext context)
        {
            _context = context;
        }

        public Task Handle(UpdateExamResults request, CancellationToken cancellationToken)
        {
            _context.SpirometryExamResults.Update(request.Results);
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
