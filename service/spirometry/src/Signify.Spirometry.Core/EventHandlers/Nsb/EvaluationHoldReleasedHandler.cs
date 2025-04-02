using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Events;
using SpiroNsb.SagaEvents;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers
{
    public class EvaluationHoldReleasedHandler : IHandleMessages<CDIEvaluationHoldReleasedEvent>
    {
        private readonly ILogger _logger;
        private readonly IMediator _mediator;
        private readonly ITransactionSupplier _transactionSupplier;

        public EvaluationHoldReleasedHandler(ILogger<EvaluationHoldReleasedHandler> logger,
            IMediator mediator,
            ITransactionSupplier transactionSupplier)
        {
            _logger = logger;
            _mediator = mediator;
            _transactionSupplier = transactionSupplier;
        }

        [Transaction]
        public async Task Handle(CDIEvaluationHoldReleasedEvent message, IMessageHandlerContext context)
        {
            var releasedDateTime = message.ReleasedOn.UtcDateTime;

            using var transaction = _transactionSupplier.BeginTransaction();

            // Update the hold released time in the DB
            var response = await _mediator.Send(new UpdateHold(message.HoldId, message.EvaluationId, releasedDateTime), context.CancellationToken);

            if (response.IsNoOp) // Sometimes CDI sends duplicate hold released events, ignore them
            {
                _logger.LogInformation("Nothing left to do with CdiHoldId={CdiHoldId} for EvaluationId={EvaluationId}",
                    message.HoldId, message.EvaluationId);
                return;
            }

            var holdReleased = response.Hold;

            var sagaHoldReleasedEvent = new HoldReleasedEvent(holdReleased.EvaluationId, releasedDateTime, holdReleased.HoldId);

            await context.SendLocal(sagaHoldReleasedEvent);

            _logger.LogInformation("Finished processing hold released event for EvaluationId={EvaluationId} and HoldId={HoldId}",
                holdReleased.EvaluationId, holdReleased.HoldId);

            await transaction.CommitAsync(context.CancellationToken);
        }
    }
}
