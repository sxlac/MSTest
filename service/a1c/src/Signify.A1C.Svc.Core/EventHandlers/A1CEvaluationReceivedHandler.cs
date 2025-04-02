using System;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.A1C.Messages.Events;
using Signify.A1C.Svc.Core.Commands;
using Signify.A1C.Svc.Core.Data;
using Signify.A1C.Svc.Core.Data.Entities;
using Signify.A1C.Svc.Core.Queries;
using System.Linq;

namespace Signify.A1C.Svc.Core.EventHandlers
{
    /// <summary>
    /// This handles Evaluation Received Event and raise A1C Performed Event.
    /// </summary>
    public class A1CEvaluationReceivedHandler : IHandleMessages<A1CEvaluationReceived>
    {
        private readonly ILogger<A1CEvaluationReceivedHandler> _logger;
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly A1CDataContext _dataContext;

        public A1CEvaluationReceivedHandler(ILogger<A1CEvaluationReceivedHandler> logger, IMediator mediator, A1CDataContext dataContext, IMapper mapper)
        {
            _logger = logger;
            _mediator = mediator;
            _mapper = mapper;
            _dataContext = dataContext;
        }

        [Transaction]
        public async Task Handle(A1CEvaluationReceived message, IMessageHandlerContext context)
        {
            _logger.LogDebug($"Start Handle A1CEvaluationReceived, EvaluationID: {message.EvaluationId}");

            //Query A1C
            _ = await _mediator.Send(new QueryA1C { AppointmentId = message.AppointmentId, Barcode = message.Barcode });

            //Get Member Information
            var getMemberInfo = _mapper.Map<GetMemberInfo>(message);
            var memberInfo = await _mediator.Send(getMemberInfo);

            //Get the NPI information
            var getProviderInfo = _mapper.Map<GetProviderInfo>(message);
            var providerInfo = await _mediator.Send(getProviderInfo);            

            var createOrUpdateA1C = _mapper.Map<CreateOrUpdateA1C>(message);

            //fill memberInfo to CreateOrUpdateA1C
            createOrUpdateA1C = _mapper.Map(memberInfo, createOrUpdateA1C);
            //fill providerInfo to CreateOrUpdateA1C
            createOrUpdateA1C = _mapper.Map(providerInfo, createOrUpdateA1C);
            createOrUpdateA1C.OrderCorrelationId = Guid.NewGuid();

            Data.Entities.A1C createA1C;

            // Create A1C row and log A1C status "A1C Performed"
            await using (var transaction = await _dataContext.Database.BeginTransactionAsync())
            {
                createA1C = await _mediator.Send(createOrUpdateA1C) ?? new Data.Entities.A1C();

                //We are commenting this as this check is for Unhappy path and a duplicate barcode in the 
                //current happy path is skipping the A1CPerformed Event.
                if (message.Performed)
                    await _mediator.Send(new CreateA1CStatus()
                    { A1CId = createA1C.A1CId, StatusCode = A1CStatusCode.A1CPerformed });
                else
                    await _mediator.Send(new CreateA1CStatus()
                    { A1CId = createA1C.A1CId, StatusCode = A1CStatusCode.A1CNotPerformed });

                await transaction.CommitAsync();
            }

            if (!message.Performed)
                return;

            if (createA1C.A1CId > 0)
            {
                var a1CPerformed = _mapper.Map<A1CPerformedEvent>(createA1C);
                a1CPerformed.ProviderName = providerInfo.FirstName + providerInfo.LastName;
                a1CPerformed.Gender = memberInfo.Gender;
                a1CPerformed.PlanId = memberInfo.PlanId;
                a1CPerformed.SubscriberId = memberInfo.SubscriberId;
                a1CPerformed.HomePhone = memberInfo.MemberPhones?.FirstOrDefault()?.PhoneNumber;
                await context.Publish(a1CPerformed);
            }

            _logger.LogDebug("End Handle EvaluationReceived");
        }
    }
}
