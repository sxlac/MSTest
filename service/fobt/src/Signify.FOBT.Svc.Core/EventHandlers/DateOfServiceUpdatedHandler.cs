using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.FOBT.Messages.Events;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Constants;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Infrastructure.Observability;

namespace Signify.FOBT.Svc.Core.EventHandlers;

/// <summary>
/// This handles DOS update of existing FOBT and saves to database
/// </summary>
public class DateOfServiceUpdateHandler : IHandleMessages<DateOfServiceUpdated>
{
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly FOBTDataContext _dataContext;
    private readonly IPublishObservability _publishObservability;
        
    public DateOfServiceUpdateHandler(ILogger<DateOfServiceUpdateHandler> logger, 
        IMediator mediator,
        FOBTDataContext dataContext, 
        IMapper mapper,
        IPublishObservability publishObservability)
    {
        _logger = logger;
        _mediator = mediator;
        _mapper = mapper;
        _dataContext = dataContext;
        _publishObservability = publishObservability;
    }

    [Transaction]
    public async Task Handle(DateOfServiceUpdated message, IMessageHandlerContext context)
    {
        var fobt = await _dataContext.FOBT
            .AsNoTracking()
            .FirstAsync(s => s.EvaluationId == message.EvaluationId, context.CancellationToken);

        fobt.DateOfService = message.DateOfService;

        var updateFobt = _mapper.Map<CreateOrUpdateFOBT>(fobt);

        await _mediator.Send(updateFobt, context.CancellationToken);

        _logger.LogInformation("DOS updated for EvaluationId {EvaluationId}", message.EvaluationId);
            
        PublishObservability(message.EvaluationId, fobt.CreatedDateTime, Observability.Evaluation.EvaluationDosUpdatedEvent);
    }
        
    private void PublishObservability(int evaluationId, DateTimeOffset createdDateTime, string eventType)
    {
        var observabilityDosUpdatedEvent = new ObservabilityEvent
        {
            EvaluationId = evaluationId,
            EventType = eventType,
            EventValue = new Dictionary<string, object>
            {
                { Observability.EventParams.EvaluationId, evaluationId },
                { Observability.EventParams.CreatedDateTime, createdDateTime.ToUnixTimeSeconds() }
            }
        };

        _publishObservability.RegisterEvent(observabilityDosUpdatedEvent, true);
    }
}