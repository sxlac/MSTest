using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Infrastructure;
using Signify.Spirometry.Core.Queries;
using SpiroEvents;
using SpiroNsb.SagaEvents;
using System.Threading.Tasks;
using Signify.Spirometry.Core.Configs.Loopback;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers
{
    /// <summary>
    /// NSB event handler for the <see cref="OverreadProcessed"/> event
    /// </summary>
    /// <remarks>
    /// This inbound event named <see cref="OverreadProcessed"/> may not be the best name here
    /// within the context of this process manager. The event is named this way because the <i>vendor</i>
    /// has processed an overread; it is not to be confused in thinking the overread has been
    /// processed by the <i>spirometry process manager</i>. To the contrary, this is what triggers
    /// the diagnosis loopback within this process manager.
    /// </remarks>
    public class OverreadProcessedByVendorHandler : IHandleMessages<OverreadProcessed>
    {
        private readonly ILogger _logger;
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly ITransactionSupplier _transactionSupplier;
        private readonly IApplicationTime _applicationTime;
        private readonly IGetLoopbackConfig _loopbackConfig;

        public OverreadProcessedByVendorHandler(ILogger<OverreadProcessedByVendorHandler> logger,
            IMediator mediator,
            IMapper mapper,
            ITransactionSupplier transactionSupplier,
            IApplicationTime applicationTime,
            IGetLoopbackConfig loopbackConfig)
        {
            _logger = logger;
            _mediator = mediator;
            _mapper = mapper;
            _transactionSupplier = transactionSupplier;
            _applicationTime = applicationTime;
            _loopbackConfig = loopbackConfig;
        }

        [Transaction]
        public async Task Handle(OverreadProcessed message, IMessageHandlerContext context)
        {
            _logger.LogInformation("Handling OverreadProcessed event with ExternalId={ExternalId}, for AppointmentId={AppointmentId}", 
                message.OverreadId, message.AppointmentId);

            var overreadResult = MapResult(message);

            using var transaction = _transactionSupplier.BeginTransaction();

            var result = await _mediator.Send(new QueryOverreadResult(message.AppointmentId), context.CancellationToken);

            if (result != null)
            {
                // Could potentially add more to the logs, or add a metric here to know how often this happens
                _logger.LogWarning("Already received an overread for AppointmentId={AppointmentId}, ignoring this overread with ExternalId={ExternalId}",
                    message.AppointmentId, message.OverreadId);
                return;
            }

            long evaluationId;
            try
            {
                evaluationId = await _mediator.Send(new QueryEvaluationId(message.AppointmentId), context.CancellationToken);
            }
            catch
            {
                if (!_loopbackConfig.CanRetryOverreadEvaluationLookup(message.ReceivedDateTime))
                {
                    // Although we aren't sending back to the queue with a delay, this will still result in the
                    // normal NSB retry with backoff. This is acceptable, though, because it'll still end up in
                    // the error queue, and there's no way I'm aware of to bypass this auto-retry for this message.
                    throw;
                }

                var delay = _loopbackConfig.OverreadEvaluationLookupRetryDelay;

                var options = new SendOptions();
                options.DelayDeliveryWith(delay);
                options.RouteToThisEndpoint();

                _logger.LogWarning("Rescheduling overread for processing (received at {OverreadReceivedAt}) with a {Delay} delay because the evaluation for AppointmentId={AppointmentId} hasn't been started/created yet",
                    message.ReceivedDateTime, delay, message.AppointmentId);

                await context.Send(message, options);

                return;
            }

            result = await _mediator.Send(new AddOverreadResult(overreadResult), context.CancellationToken);

            await context.SendLocal(new OverreadReceivedEvent
            {
                EvaluationId = evaluationId,
                CreatedDateTime = result.CreatedDateTime,
                OverreadResultId = result.OverreadResultId
            });

            _logger.LogInformation("Finished handling OverreadProcessed event with ExternalId={ExternalId}, for AppointmentId={AppointmentId}, and EvaluationId={EvaluationId}",
                message.OverreadId, message.AppointmentId, evaluationId);

            await transaction.CommitAsync(context.CancellationToken);
        }

        private OverreadResult MapResult(OverreadProcessed message)
        {
            var overreadResult = _mapper.Map<OverreadResult>(message);

            overreadResult.NormalityIndicatorId = GetNormality(message).NormalityIndicatorId;
            overreadResult.CreatedDateTime = _applicationTime.UtcNow();

            return overreadResult;
        }

        private NormalityIndicator GetNormality(OverreadProcessed message)
        {
            switch (message.ObstructionPerOverread?.ToUpper())
            {
                case "YES":
                    return NormalityIndicator.Abnormal;
                case "NO":
                    return NormalityIndicator.Normal;
                case "INCONCLUSIVE": // This is the one we expect from Nuvo, but keeping the others for flexibility
                case "UNDETERMINED":
                case "INDETERMINATE":
                    return NormalityIndicator.Undetermined;
            }

            _logger.LogWarning("Unable to determine normality for ObstructionPerOverread={ObstructionPerOverread}, for AppointmentId={AppointmentId}, for ExternalId={ExternalId}",
                message.ObstructionPerOverread, message.AppointmentId, message.OverreadId);

            return NormalityIndicator.Undetermined;
        }
    }
}
