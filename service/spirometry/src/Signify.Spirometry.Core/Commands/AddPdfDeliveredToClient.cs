using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.Data;
using System.Threading;
using System.Threading.Tasks;

using PdfDeliveredToClient = SpiroEvents.PdfDeliveredToClient;

namespace Signify.Spirometry.Core.Commands
{
    /// <summary>
    /// Command to save a <see cref="Data.Entities.PdfDeliveredToClient"/> to database
    /// </summary>
    public class AddPdfDeliveredToClient : IRequest<Data.Entities.PdfDeliveredToClient>
    {
        public PdfDeliveredToClient Event { get; }

        public AddPdfDeliveredToClient(PdfDeliveredToClient @event)
        {
            Event = @event;
        }
    }

    public class AddPdfDeliveredToClientHandler : IRequestHandler<AddPdfDeliveredToClient, Data.Entities.PdfDeliveredToClient>
    {
        private readonly ILogger _logger;
        private readonly SpirometryDataContext _dataContext;
        private readonly IMapper _mapper;

        public AddPdfDeliveredToClientHandler(ILogger<AddPdfDeliveredToClientHandler> logger,
            SpirometryDataContext dataContext,
            IMapper mapper)
        {
            _logger = logger;
            _dataContext = dataContext;
            _mapper = mapper;
        }

        public async Task<Data.Entities.PdfDeliveredToClient> Handle(AddPdfDeliveredToClient request, CancellationToken cancellationToken)
        {
            var entity = await FindExisting(request.Event, cancellationToken);

            if (entity != null)
            {
                _logger.LogInformation("A PdfDeliveredToClient record for event already exists for EvaluationId={EvaluationId}, with PdfDeliveredToClientId={PdfDeliveredToClientId}",
                    request.Event.EvaluationId, entity.PdfDeliveredToClientId);
                return entity;
            }

            entity = _mapper.Map<Data.Entities.PdfDeliveredToClient>(request.Event);

            // If for some reason a SpirometryExam for this EvaluationId doesn't exist, this will throw an exception due to FK constraint, which will result in NSB retry
            entity = (await _dataContext.PdfDeliveredToClients.AddAsync(entity, cancellationToken)).Entity;

            await _dataContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully inserted a new PdfDeliveredToClient record for EvaluationId={EvaluationId}, new PdfDeliveredToClientId={PdfDeliveredToClientId}",
                request.Event.EvaluationId, entity.PdfDeliveredToClientId);

            return entity;
        }

        private async Task<Data.Entities.PdfDeliveredToClient> FindExisting(PdfDeliveredToClient @event, CancellationToken cancellationToken)
        {
            return await _dataContext.PdfDeliveredToClients
                .AsNoTracking()
                .FirstOrDefaultAsync(each => each.EvaluationId == @event.EvaluationId
                                             && each.EventId == @event.EventId, cancellationToken);
        }
    }
}
