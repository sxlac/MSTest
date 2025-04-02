using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Events;
using SpiroNsb.SagaEvents;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers
{
    public class EvaluationHeldHandler : IHandleMessages<CDIEvaluationHeldEvent>
    {
        private readonly ILogger _logger;
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly ITransactionSupplier _transactionSupplier;

        public EvaluationHeldHandler(ILogger<EvaluationHeldHandler> logger,
            IMediator mediator,
            IMapper mapper,
            ITransactionSupplier transactionSupplier)
        {
            _logger = logger;
            _mediator = mediator;
            _mapper = mapper;
            _transactionSupplier = transactionSupplier;
        }

        [Transaction]
        public async Task Handle(CDIEvaluationHeldEvent message, IMessageHandlerContext context)
        {
            using var transaction = _transactionSupplier.BeginTransaction();

            var holdMapped = _mapper.Map<Hold>(message);

            // Save the new hold to db
            var response = await _mediator.Send(new AddHold(holdMapped), context.CancellationToken);

            if (!response.IsNew) // Sometimes CDI sends duplicate hold events, ignore them
            {
                _logger.LogInformation("Hold for EvaluationId={EvaluationId} was already received, nothing left to do", message.EvaluationId);
                return;
            }

            var hold = response.Hold;

            var sagaHoldCreatedEvent = new HoldCreatedEvent(hold.EvaluationId, hold.CreatedDateTime, hold.HoldId);

            await context.SendLocal(sagaHoldCreatedEvent);

            _logger.LogInformation("Finished handling evaluation held event for EvaluationId={EvaluationId} and HoldId={HoldId}",
                hold.EvaluationId, hold.HoldId);

            await transaction.CommitAsync(context.CancellationToken);
        }
    }
}
