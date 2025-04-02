using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.HBA1CPOC.Svc.Core.Data;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.HBA1CPOC.Svc.Core.Commands
{
    /// <summary>
    /// Command to add details about why a HBA1CPOC exam was not performed to database
    /// </summary>
    public class AddHba1CpocNotPerformed : IRequest<Hba1CpocNotPerformed>
    {
        public Data.Entities.HBA1CPOC HBA1CPOC { get; set; }
        public NotPerformedReason NotPerformedReason { get; set; }
        public string Notes { get; set; }
    }

    public class AddHba1CpocNotPerformedHandler : IRequestHandler<AddHba1CpocNotPerformed, Hba1CpocNotPerformed>
    {
        private readonly ILogger _logger;
        private readonly Hba1CpocDataContext _context;
        private readonly IMapper _mapper;

        public AddHba1CpocNotPerformedHandler(ILogger<AddHba1CpocNotPerformedHandler> logger,
            Hba1CpocDataContext context,
            IMapper mapper)
        {
            _logger = logger;
            _context = context;
            _mapper = mapper;
        }

        public async Task<Hba1CpocNotPerformed> Handle(AddHba1CpocNotPerformed request, CancellationToken cancellationToken)
        {
            var reason = await _context.HBA1CPOCNotPerformed.AsNoTracking().FirstOrDefaultAsync(e => e.HBA1CPOCId == request.HBA1CPOC.HBA1CPOCId, cancellationToken);

            if (reason == null)
            {
                var entity = _mapper.Map<Hba1CpocNotPerformed>(request.NotPerformedReason);
                entity.HBA1CPOCId = request.HBA1CPOC.HBA1CPOCId;
                entity.Notes = request.Notes;
                entity = (await _context.HBA1CPOCNotPerformed.AddAsync(entity, cancellationToken)).Entity;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully added a new HBA1CPOCNotPerformed record for HBA1CPOCId={HBA1CPOCId}; new HBA1CPOCNotPerformedId={HBA1CPOCNotPerformedId}",
                    entity.HBA1CPOCId, entity.HBA1CPOCNotPerformedId);

                return entity;
            }

            _logger.LogInformation("HBA1CPOCNotPerformed record already exists for HBA1CPOCId={HBA1CPOCId}; new HBA1CPOCNotPerformedId={HBA1CPOCNotPerformedId}",
                  reason.HBA1CPOCId, reason.HBA1CPOCNotPerformedId);

            return reason;

        }
    }
}
