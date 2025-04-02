using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.eGFR.Core.Data;
using System.Threading;
using System.Threading.Tasks;

using PdfDeliveredToClient = EgfrEvents.PdfDeliveredToClient;

namespace Signify.eGFR.Core.Commands;

/// <summary>
/// Command to save a <see cref="Data.Entities.PdfDeliveredToClient"/> to database
/// </summary>
public class AddPdfDeliveredToClient(PdfDeliveredToClient @event) : IRequest<Data.Entities.PdfDeliveredToClient>
{
    public PdfDeliveredToClient Event { get; } = @event;
}

public class AddPdfDeliveredToClientHandler(
    ILogger<AddPdfDeliveredToClientHandler> logger,
    DataContext dataContext,
    IMapper mapper)
    : IRequestHandler<AddPdfDeliveredToClient, Data.Entities.PdfDeliveredToClient>
{
    private readonly ILogger _logger = logger;

    public async Task<Data.Entities.PdfDeliveredToClient> Handle(AddPdfDeliveredToClient request, CancellationToken cancellationToken)
    {
        var entity = await FindExisting(request.Event, cancellationToken);

        if (entity != null)
        {
            _logger.LogInformation("A PdfDeliveredToClient record for event already exists for EvaluationId={EvaluationId}, with PdfDeliveredToClientId={PdfDeliveredToClientId}",
                request.Event.EvaluationId, entity.PdfDeliveredToClientId);
            return entity;
        }

        entity = mapper.Map<Data.Entities.PdfDeliveredToClient>(request.Event);

        // If for some reason an Exam for this EvaluationId doesn't exist, this will throw an exception due to FK constraint, which will result in NSB retry
        entity = (await dataContext.PdfDeliveredToClients.AddAsync(entity, cancellationToken)).Entity;

        await dataContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully inserted a new PdfDeliveredToClient record for EvaluationId={EvaluationId}, new PdfDeliveredToClientId={PdfDeliveredToClientId}",
            request.Event.EvaluationId, entity.PdfDeliveredToClientId);

        return entity;
    }

    private async Task<Data.Entities.PdfDeliveredToClient> FindExisting(PdfDeliveredToClient @event,
        CancellationToken cancellationToken)
        => await dataContext.PdfDeliveredToClients
            .AsNoTracking()
            .FirstOrDefaultAsync(each => each.EvaluationId == @event.EvaluationId
                                         && each.EventId == @event.EventId, cancellationToken);
}