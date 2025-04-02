using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.FOBT.Messages.Events;
using Signify.FOBT.Svc.Core.ApiClient;
using Signify.FOBT.Svc.Core.ApiClient.Requests;
using Signify.FOBT.Svc.Core.Commands;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Queries;
using System;
using System.Threading.Tasks;
using Signify.FOBT.Svc.Core.Exceptions;

namespace Signify.FOBT.Svc.Core.EventHandlers;

/// <summary>
/// Populates order details and posts to LabsAPI
/// </summary>
public class CreateOrderHandler : IHandleMessages<CreateOrderEvent>
{
    private readonly IMapper _mapper;
    private readonly ILogger<CreateOrderHandler> _logger;
    private readonly IMediator _mediator;
    private readonly ILabsApi _labsApi;
    private const string PROD_CODE = "FOBT";

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
        _logger.LogDebug("Start CreateOrderEvent Handler, EvaluationID: {EvaluationId}", message.EvaluationId);


        //Create order 
        //Get information to populate from FOBT 
        var fobtQuery = new GetFOBT { EvaluationId = Convert.ToInt64(message.EvaluationId) };
        var fobt = await _mediator.Send(fobtQuery, context.CancellationToken);
        var msg = _mapper.Map<CreateOrderEvent>(fobt);
        msg.PlanId = message.PlanId;
        msg.ProviderName = message.ProviderName;
        msg.Sex = message.Sex;
        msg.LabTestType = PROD_CODE;
        msg.SubscriberId = message.SubscriberId;
        msg.HomePhone = message.HomePhone;

        //Call LabsAPI 
        var request = _mapper.Map<CreateOrder>(msg);
        request.MemberFlag = "true";

        var response = await _labsApi.CreateOrder(request);

        if (!response.IsSuccessStatusCode || response.Content <= 0)
        {
            //Failed Order creation. Throw exception to hit retry process
            throw new CreateOrderException(Convert.ToInt64(msg.EvaluationId), response.StatusCode, response.Error);
        }

        if (Convert.ToInt32(response.Content) > 0)
        {
            await _mediator.Send(new CreateFOBTStatus { FOBT = fobt, StatusCode = FOBTStatusCode.LabOrderCreated }, context.CancellationToken);
        }

        _logger.LogDebug("End Handle CreateOrderHandler, EvaluationId: {EvaluationId}", msg.EvaluationId);
    }
}