using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.FOBT.Messages.Events;
using Signify.FOBT.Svc.Core.Constants;
using Signify.FOBT.Svc.Core.Infrastructure.Observability;

namespace Signify.FOBT.Svc.Core.EventHandlers;

/// <summary>
///This handles FOBT Performed event. It updates inventory and logs the status change InventoryUpdateRequested.
/// </summary>
public class FOBTPerformedHandler : IHandleMessages<FOBTPerformedEvent>
{
    private readonly IMapper _mapper;
    private readonly ILogger<FOBTPerformedHandler> _logger;
    private readonly IPublishObservability _publishObservability;

    public FOBTPerformedHandler(ILogger<FOBTPerformedHandler> logger, 
        IMapper mapper,
        IPublishObservability publishObservability)
    {
        _logger = logger;
        _mapper = mapper;
        _publishObservability = publishObservability;
    }

    [Transaction]
    public async Task Handle(FOBTPerformedEvent message, IMessageHandlerContext context)
    {
        _logger.LogDebug($"Start FOBTPerformedEvent Handler, EvaluationID: {message.EvaluationId}");

        //Update Inventory
        //var updateInventory = _mapper.Map<UpdateInventoryRequest>(message);
          
        //Send InventoryUpdate command via NServiceBus
        //await context.Send(updateInventory);

        //Create order 
        var createOrder = new CreateOrderEvent
        {
            EvaluationId = message.EvaluationId.ToString(),
            PlanId = message.PlanId,
            ProviderName = message.ProviderName,
            SubscriberId = message.SubscriberId,
            HomePhone = message.HomePhone,
            AppointmentId = message.AppointmentId,
            ClientId = message.ClientId
        };

        var validGender = !string.IsNullOrWhiteSpace(message.Gender);
        _logger.LogInformation($" {(validGender ? "CreateOrderEvent has non-empty Gender Field." : "CreateOrderEvent has empty Gender Field")}");
        if (validGender)
        {
            createOrder.Sex = string.Compare(message.Gender, "male", StringComparison.OrdinalIgnoreCase) == 0 ? 'M' : 'F';
        }
        await context.Send(createOrder);

        _logger.LogDebug("End Handle FOBTPerformedEvent, EvaluationID: {EvaluationId}", message.EvaluationId);
            
        PublishObservability(message, Observability.Evaluation.EvaluationPerformedEvent);
    }
    private void PublishObservability(FOBTPerformedEvent message, string eventType)
    {
        var observabilityReceivedEvent = new ObservabilityEvent
        {
            EvaluationId = message.EvaluationId,
            EventType = eventType, 
            EventValue = new Dictionary<string, object>
            {
                { Observability.EventParams.EvaluationId, message.EvaluationId },
                { Observability.EventParams.CreatedDateTime, message.CreatedDateTime.ToUnixTimeSeconds() }
            }
        };

        _publishObservability.RegisterEvent(observabilityReceivedEvent, true);
    }
}