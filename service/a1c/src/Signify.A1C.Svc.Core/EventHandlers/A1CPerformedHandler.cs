using AutoMapper;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.A1C.Messages.Events;
using Signify.A1C.Svc.Core.ApiClient.Requests;
using System.Threading.Tasks;

namespace Signify.A1C.Svc.Core.EventHandlers
{
    /// <summary>
    ///This handles A1C Performed event. It updates inventory and logs the status change InventoryUpdateRequested.
    /// </summary>
    public class A1CPerformedHandler : IHandleMessages<A1CPerformedEvent>
    {
        private readonly IMapper _mapper;
        private readonly ILogger<A1CPerformedHandler> _logger;

        public A1CPerformedHandler(ILogger<A1CPerformedHandler> logger, IMapper mapper)
        {
            _logger = logger;
            _mapper = mapper;
        }

        [Transaction]
        public async Task Handle(A1CPerformedEvent message, IMessageHandlerContext context)
        {
            _logger.LogDebug($"Start A1CPerformedEvent Handler, EvaluationID: {message.EvaluationId}");            

            //Update inventory
            var updateInventory = _mapper.Map<UpdateInventoryRequest>(message);
            //Send InventoryUpdate command via NServiceBus
            await context.Send(updateInventory);

            //Create order 
            CreateOrderEvent createOrder = new CreateOrderEvent();
            createOrder.EvaluationId = message.EvaluationId.ToString();
            createOrder.PlanId = message.PlanId;
            createOrder.ProviderName = message.ProviderName;
            createOrder.SubscriberId = message.SubscriberId;
            createOrder.HomePhone = message.HomePhone;
            createOrder.AppointmentId = message.AppointmentId;
            createOrder.ClientId = message.ClientId;
            if (!string.IsNullOrEmpty(message.Gender) && string.Compare(message.Gender, "Male", true) == 0)
                createOrder.Sex = 'M';
            else if (!string.IsNullOrEmpty(message.Gender) && string.Compare(message.Gender, "Female", true) == 0)
                createOrder.Sex = 'F';

            await context.Send(createOrder);

            _logger.LogDebug($"End Handle A1CPerformedEvent, EvaluationID: {message.EvaluationId}");
        }
    }
}