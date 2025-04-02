using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.FOBT.Messages.Events;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Queries;
using Signify.FOBT.Messages.Events.Status;

using System.Linq;
using Signify.FOBT.Svc.Core.Constants;
using Signify.FOBT.Svc.Core.Infrastructure.Observability;

namespace Signify.FOBT.Svc.Core.EventHandlers;

/// <summary>
/// This handles Evaluation Received Event and raise FOBT Performed Event.
/// </summary>
public class EvaluationReceivedHandler : IHandleMessages<EvaluationReceived>
{
    private readonly ILogger<EvaluationReceivedHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly FOBTDataContext _dataContext;
    private readonly IPublishObservability _publishObservability;
        
    public EvaluationReceivedHandler(ILogger<EvaluationReceivedHandler> logger, 
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
    public async Task Handle(EvaluationReceived evt, IMessageHandlerContext context)
    {
        _logger.LogDebug("Start Handle FOBTEvaluationReceived for EvaluationId {EvaluationId}", evt.EvaluationId);

        //Get Member Information
        var memberInfoRequest = _mapper.Map<GetMemberInfo>(evt);
        var memberInfo = await _mediator.Send(memberInfoRequest, context.CancellationToken);

        //Get the NPI information
        var getProviderInfo = _mapper.Map<GetProviderInfo>(evt);
        var providerInfo = await _mediator.Send(getProviderInfo, context.CancellationToken);

        var createOrUpdateFobt = _mapper.Map<CreateOrUpdateFOBT>(evt);

        //fill memberInfo to CreateOrUpdateFOBT
        createOrUpdateFobt = _mapper.Map(memberInfo, createOrUpdateFobt);
        //fill providerInfo to CreateOrUpdateFOBT
        createOrUpdateFobt = _mapper.Map(providerInfo, createOrUpdateFobt);
        createOrUpdateFobt.OrderCorrelationId = Guid.NewGuid();

        var status = evt.Performed ? FOBTStatusCode.FOBTPerformed : FOBTStatusCode.FOBTNotPerformed;

        await using var transaction = await _dataContext.Database.BeginTransactionAsync(context.CancellationToken).ConfigureAwait(false);
        var fobt = await _mediator.Send(createOrUpdateFobt, context.CancellationToken);
        await _mediator.Send(new CreateFOBTStatus
        {
            FOBT = fobt,
            StatusCode = status
        }, context.CancellationToken);
        await transaction.CommitAsync(context.CancellationToken);

        if (!evt.Performed)
        {
            var fobtNotPerformed = _mapper.Map<NotPerformed>(fobt);

            var notPerformedReasonResult = await _mediator.Send(new GetNotPerformedReason { EvaluationId = evt.EvaluationId }, context.CancellationToken);
            if (notPerformedReasonResult != null)
            {
                await _mediator.Send(new AddFOBTNotPerformed
                {
                    NotPerformedReason = notPerformedReasonResult.NotPerformedReason,
                    FOBT = fobt,
                    Notes = notPerformedReasonResult.ReasonNotes
                }, context.CancellationToken);

                fobtNotPerformed = _mapper.Map(notPerformedReasonResult, fobtNotPerformed);
            }
            else
            {
                PublishObservability(evt, Observability.Evaluation.EvaluationUndefinedEvent);
            }

            await _mediator.Send(new PublishStatusUpdate(fobtNotPerformed), context.CancellationToken);
            PublishObservability(evt, Observability.Evaluation.EvaluationNotPerformedEvent);
            return;
        }

        var fobtPerformed = _mapper.Map<Performed>(fobt);
        await _mediator.Send(new PublishStatusUpdate(fobtPerformed), context.CancellationToken);
            
        if (fobt.FOBTId > 0)
        {
            var fobtPerformedEvent = _mapper.Map<FOBTPerformedEvent>(fobt);
            fobtPerformedEvent.ProviderName = providerInfo.FirstName + providerInfo.LastName;
            fobtPerformedEvent.Gender = memberInfo.Gender;
            fobtPerformedEvent.PlanId = memberInfo.PlanId;
            fobtPerformedEvent.SubscriberId = memberInfo.SubscriberId;
            fobtPerformedEvent.HomePhone = memberInfo.MemberPhones?.FirstOrDefault()?.PhoneNumber;

            await context.Publish(fobtPerformedEvent);
        }

        _logger.LogDebug("End Handle EvaluationReceived for EvaluationId {EvaluationId}", evt.EvaluationId);
            
        PublishObservability(evt, Observability.Evaluation.EvaluationReceivedEvent);
    }     
    private void PublishObservability(EvaluationReceived message, string eventType)
    {
        var observabilityEvent = new ObservabilityEvent
        {
            EvaluationId = message.EvaluationId,
            EventType = eventType, 
            EventValue = new Dictionary<string, object>
            {
                { Observability.EventParams.EvaluationId, message.EvaluationId },
                { Observability.EventParams.CreatedDateTime, message.CreatedDateTime.ToUnixTimeSeconds() }
            }
        };

        _publishObservability.RegisterEvent(observabilityEvent, true);
    }
}