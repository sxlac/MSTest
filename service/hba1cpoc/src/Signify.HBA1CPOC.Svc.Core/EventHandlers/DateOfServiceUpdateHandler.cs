using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.HBA1CPOC.Messages.Events;
using Signify.HBA1CPOC.Svc.Core.Commands;
using Signify.HBA1CPOC.Svc.Core.Data;

namespace Signify.HBA1CPOC.Svc.Core.EventHandlers;

/// <summary>
/// This handles DOS update of existing HBA1COPOC and saves to database
/// </summary>
public class DateOfServiceUpdateHandler : IHandleMessages<DateOfServiceUpdated>
{
    private readonly ILogger<DateOfServiceUpdateHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly Hba1CpocDataContext _dataContext;

    public DateOfServiceUpdateHandler(ILogger<DateOfServiceUpdateHandler> logger, IMediator mediator,
        Hba1CpocDataContext dataContext, IMapper mapper)
    {
        _logger = logger;
        _mediator = mediator;
        _mapper = mapper;
        _dataContext = dataContext;
    }

    [Transaction]
    public async Task Handle(DateOfServiceUpdated message, IMessageHandlerContext context)
    {
        //Guaranteed that this evaluationId exists in DB, otherwise NSB would not have published
        var hba1cpoc = await _dataContext.HBA1CPOC.AsNoTracking()
            .FirstOrDefaultAsync(s => s.EvaluationId == message.EvaluationId, context.CancellationToken);

        var oldDos = hba1cpoc.DateOfService;
        hba1cpoc.DateOfService = message.DateOfService;
        var updateHba1Cpoc = _mapper.Map<CreateOrUpdateHBA1CPOC>(hba1cpoc);
        await _mediator.Send(updateHba1Cpoc, context.CancellationToken);
        _logger.LogInformation(
            "DOS updated for existing Evaluation, EvaluationID={EvaluationId}, previous DOS={oldDos}", message.EvaluationId, oldDos);
    }
}