using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.A1C.Messages.Events;
using Signify.A1C.Svc.Core.ApiClient;
using Signify.A1C.Svc.Core.ApiClient.Requests;
using Signify.A1C.Svc.Core.Commands;
using Signify.A1C.Svc.Core.Data.Entities;
using Signify.A1C.Svc.Core.Queries;
using System;
using System.Threading.Tasks;

namespace Signify.A1C.Svc.Core.EventHandlers
{
    /// <summary>
    /// 
    /// Populates order details and posts to LabsAPI
    /// </summary>
    public class CreateOrderHandler : IHandleMessages<CreateOrderEvent>
    {
        private readonly IMapper _mapper;
        private readonly ILogger<CreateOrderHandler> _logger;
        private readonly IMediator _mediator;
        private readonly ILabsApi _labsApi;
        private const string PROD_CODE = "HBA1C";

        public CreateOrderHandler(ILogger<CreateOrderHandler> logger, IMapper mapper, IMediator mediator, ILabsApi labsApi)
        {
            _logger = logger;
            _mapper = mapper;
            _mediator = mediator;
            _labsApi = labsApi;
        }

        [Transaction]
        public async Task Handle(CreateOrderEvent message, IMessageHandlerContext context)
        {
            _logger.LogDebug($"Start CreateOrderEvent Handler, EvaluationID: {message.EvaluationId}");

            //Create order 
            //Get information to populate from A1C since DOS is available only in A1C
            var a1cQuery = new GetA1C { EvaluationId = Convert.ToInt32(message.EvaluationId) };
            var a1c = await _mediator.Send(a1cQuery);
            var msg = _mapper.Map<CreateOrderEvent>(a1c);
            msg.PlanId = message.PlanId;
            msg.ProviderName = message.ProviderName;
            msg.Sex = message.Sex;
            msg.LabTestType = PROD_CODE;
            msg.SubscriberId = message.SubscriberId;
            msg.HomePhone = message.HomePhone;

            //Call LabsAPI 
            CreateOrder request = _mapper.Map<CreateOrder>(msg);
            request.MemberFlag = "true";

            var response = await _labsApi.CreateOrder(request);
            if (!response.IsSuccessStatusCode || response.Content <= 0)
            {
                //Failed Order creation. Throw exception to hit retry process
                throw new ApplicationException(
                    $"Unable to create Order. EvaluationId: {msg.EvaluationId}");
            }
            else if (Convert.ToInt32(response.Content) > 0)
            {
                await _mediator.Send(new CreateA1CStatus()
                { A1CId = a1c.A1CId, StatusCode = A1CStatusCode.LabOrderCreated });
            }

            _logger.LogDebug($"End Handle CreateOrderHandler, EvaluationId: {msg.EvaluationId}");
        }
    }
}
