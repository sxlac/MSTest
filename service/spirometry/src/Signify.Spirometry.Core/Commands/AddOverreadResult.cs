using MediatR;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.Spirometry.Core.Commands
{
    public class AddOverreadResult : IRequest<OverreadResult>
    {
        public OverreadResult Result { get; }

        public AddOverreadResult(OverreadResult result)
        {
            Result = result;
        }
    }

    public class AddOverreadResultHandler : IRequestHandler<AddOverreadResult, OverreadResult>
    {
        private readonly ILogger _logger;
        private readonly SpirometryDataContext _context;

        public AddOverreadResultHandler(ILogger<AddOverreadResultHandler> logger,
            SpirometryDataContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<OverreadResult> Handle(AddOverreadResult request, CancellationToken cancellationToken)
        {
            var entity = (await _context.OverreadResults.AddAsync(request.Result, cancellationToken)).Entity;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully inserted a new OverreadResult record for AppointmentId={AppointmentId}, with new OverreadResultId={OverreadResultId}",
                request.Result.AppointmentId, entity.OverreadResultId);

            return entity;
        }
    }
}
