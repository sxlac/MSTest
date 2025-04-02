using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.Spirometry.Core.Configs.Loopback;
using Signify.Spirometry.Core.Infrastructure;
using SpiroNsb.SagaCommands;
using SpiroNsb.SagaEvents;
using Signify.Spirometry.Core.Sagas.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace SpiroNsb.Sagas
{
    public class EvaluationSaga : Saga<EvaluationSagaData>,
        // Started by
        IAmStartedByMessages<EvaluationProcessedEvent>,
        IAmStartedByMessages<HoldCreatedEvent>,
        IAmStartedByMessages<HoldReleasedEvent>, // Edge case, receiving HoldReleased before HoldCreated
        IAmStartedByMessages<OverreadReceivedEvent>,
        IAmStartedByMessages<PdfDeliveredToClientEvent>,
        // Handle
        IHandleMessages<OverreadProcessedEvent>,
        IHandleMessages<FlagCreatedEvent>,
        IHandleMessages<PdfDeliveryProcessedEvent>
    {
        private readonly ILogger _logger;
        private readonly IGetLoopbackConfig _config;
        private readonly IApplicationTime _applicationTime;

        public EvaluationSaga(ILogger<EvaluationSaga> logger,
            IGetLoopbackConfig config,
            IApplicationTime applicationTime)
        {
            _logger = logger;
            _config = config;
            _applicationTime = applicationTime;
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<EvaluationSagaData> mapper)
        {
            mapper.MapSaga(saga => saga.EvaluationId)
                .ToMessage<EvaluationProcessedEvent>(evt => evt.EvaluationId)
                .ToMessage<HoldCreatedEvent>(evt => evt.EvaluationId)
                .ToMessage<HoldReleasedEvent>(evt => evt.EvaluationId)
                .ToMessage<OverreadReceivedEvent>(evt => evt.EvaluationId)
                .ToMessage<PdfDeliveredToClientEvent>(evt => evt.EvaluationId)
                .ToMessage<OverreadProcessedEvent>(evt => evt.EvaluationId)
                .ToMessage<FlagCreatedEvent>(evt => evt.EvaluationId)
                .ToMessage<PdfDeliveryProcessedEvent>(evt => evt.EvaluationId);
        }

        #region IHandleMessages
        [Transaction]
        public async Task Handle(EvaluationProcessedEvent message, IMessageHandlerContext context)
        {
            _logger.LogInformation("Received {EventType} for EvaluationId={EvaluationId}",
                nameof(EvaluationProcessedEvent), message.EvaluationId);

            Data.IsPerformed = message.IsPerformed;
            Data.SpirometryExamId = message.SpirometryExamId;
            Data.FinalizedProcessedDateTime = message.CreatedDateTime;

            if (!message.IsPerformed)
            {
                // Although the handler that raised this event should have set the NeedsOverread property on the
                // message correctly, we know these should always be false when NotPerformed, so just hard-code it
                Data.NeedsOverread = Data.NeedsFlag = Data.IsBillable = false;
            }
            else
            {
                // We always know whether we need an overread at time of POC (A/B/C vs D/E/F)
                Data.NeedsOverread = message.NeedsOverread;

                // With current business rules, will only ever be `null` (needs to wait for an overread) or `false` (Scenario 2)
                Data.NeedsFlag = message.NeedsFlag;

                // Data.IsBillable is definitely `null` before receiving this event, because it's the first event
                // that can possibly be received that has an IsBillable property (Finalized and OverreadProcessed
                // (which is triggered by this saga only once a Finalized is received) are the only events with a
                // IsBillable flag).
                Data.IsBillable = message.IsBillable;
            }

            await EnqueueOutstandingTasks(context);

            // Potential edge case where we may be done processing:
            // Hold is already expired, pdfdelivery already received, overread already received and
            // clinically-invalid so not billable
            MarkAsCompleteIfDone();
        }

        [Transaction]
        public async Task Handle(HoldCreatedEvent message, IMessageHandlerContext context)
        {
            _logger.LogInformation("Received {EventType} for EvaluationId={EvaluationId}",
                nameof(HoldCreatedEvent), message.EvaluationId);

            Data.HoldId = message.HoldId;
            Data.HoldCreatedDateTime = message.CreatedDateTime;

            await EnqueueOutstandingTasks(context);

            // Unlikely edge case, if failed to insert hold to db when receiving HoldCreated event from CDI,
            // and the HoldExpiredEvent was already processed, and reprocessing the HoldCreated from error queue
            MarkAsCompleteIfDone();
        }

        [Transaction]
        public async Task Handle(OverreadReceivedEvent message, IMessageHandlerContext context)
        {
            _logger.LogInformation("Received {EventType} for EvaluationId={EvaluationId}",
                nameof(OverreadReceivedEvent), message.EvaluationId);

            Data.OverreadResultId = message.OverreadResultId;
            Data.OverreadReceivedDateTime = message.CreatedDateTime;

            await EnqueueOutstandingTasks(context);

            MarkAsCompleteIfDone();
        }

        [Transaction]
        public async Task Handle(OverreadProcessedEvent message, IMessageHandlerContext context)
        {
            _logger.LogInformation("Received {EventType} for EvaluationId={EvaluationId}",
                nameof(OverreadProcessedEvent), message.EvaluationId);

            Data.OverreadProcessedDateTime = message.CreatedDateTime;
            Data.IsBillable = message.IsBillable;
            Data.NeedsFlag = message.NeedsFlag;

            await EnqueueOutstandingTasks(context);

            MarkAsCompleteIfDone();
        }

        [Transaction]
        public async Task Handle(FlagCreatedEvent message, IMessageHandlerContext context)
        {
            _logger.LogInformation("Received {EventType} for EvaluationId={EvaluationId}",
                nameof(FlagCreatedEvent), message.EvaluationId);

            Data.ClarificationFlagId = message.ClarificationFlagId;
            Data.FlagCreatedDateTime = message.CreatedDateTime;

            await EnqueueOutstandingTasks(context);

            MarkAsCompleteIfDone();
        }

        [Transaction]
        public async Task Handle(HoldReleasedEvent message, IMessageHandlerContext context)
        {
            const string eventType = nameof(HoldReleasedEvent);

            _logger.LogInformation("Received {EventType} for EvaluationId={EvaluationId}",
                eventType, message.EvaluationId);

            // Unlikely, but it's possible we could receive HoldReleased before HoldCreated, so set the HoldId here
            Data.HoldId = message.HoldId;
            Data.HoldReleasedDateTime = message.CreatedDateTime;

            await EnqueueOutstandingTasks(context);

            MarkAsCompleteIfDone();
        }

        [Transaction]
        public async Task Handle(PdfDeliveredToClientEvent message, IMessageHandlerContext context)
        {
            const string eventType = nameof(PdfDeliveredToClientEvent);

            _logger.LogInformation("Received {EventType} for EvaluationId={EvaluationId}, with PdfDeliveredToClientId={PdfDeliveredToClientId}",
                eventType, message.EvaluationId, message.PdfDeliveredToClientId);

            // We can receive multiple pdfdelivery events for a given evaluation; the first one we get should be used for billing, and any subsequent ones ignored
            if (Data.PdfDeliveredToClientId.HasValue)
            {
                if (Data.PdfDeliveredToClientId.Value != message.PdfDeliveredToClientId)
                {
                    _logger.LogInformation("Already received {EventType} for EvaluationId={EvaluationId} ignoring PdfDeliveredToClientId={PdfDeliveredToClientId}",
                        eventType, message.EvaluationId, message.PdfDeliveredToClientId);
                }

                return; // Nothing to do
            }

            Data.PdfDeliveredToClientId = message.PdfDeliveredToClientId;
            Data.PdfDeliveredToClientDateTime = message.CreatedDateTime;

            if (!HasProcessedFinalized)
            {
                // This truly is an abnormal scenario. PdfDelivery doesn't happen until after the evaluation
                // has passed CDI and Coding, which normally is days after the evaluation was Finalized.
                _logger.LogWarning("Received {EventType} for EvaluationId={EvaluationId}, but a Finalized event has not been processed yet; either the Finalized event was missed, or it's sitting in the error queue",
                    eventType, message.EvaluationId);
            }

            await EnqueueOutstandingTasks(context);

            MarkAsCompleteIfDone();
        }

        [Transaction]
        public async Task Handle(PdfDeliveryProcessedEvent message, IMessageHandlerContext context)
        {
            _logger.LogInformation("Received {EventType} for EvaluationId={EvaluationId}",
                nameof(PdfDeliveryProcessedEvent), message.EvaluationId);

            Data.PdfDeliveryProcessedDateTime = message.CreatedDateTime;

            await EnqueueOutstandingTasks(context);

            MarkAsCompleteIfDone();
        }
        #endregion IHandleMessages

        private void MarkAsCompleteIfDone()
        {
            if (!ShouldBeMarkedAsComplete)
                return;

            _logger.LogInformation("Marking saga for EvaluationId={EvaluationId} as complete", Data.EvaluationId);

            MarkAsComplete();
        }

        #region Rules
        #region Convenience
        private bool HasProcessedFinalized => Data.IsPerformed.HasValue;

        private bool HasReceivedPdfDelivery => Data.PdfDeliveredToClientId.HasValue;

        private bool HasProcessedPdfDelivery => Data.PdfDeliveryProcessedDateTime.HasValue;
        #endregion Convenience

        /// <summary>
        /// Whether or not this saga should be marked as Complete
        /// </summary>
        private bool ShouldBeMarkedAsComplete =>
            HasProcessedPdfDelivery; // This is the final command that gets triggered; once this happens there's nothing left to do

        private bool ShouldProcessOverread =>
            _config.ShouldProcessOverreads && // Enabled
            HasProcessedFinalized && // Finalized
            Data.NeedsOverread is true && // Needs overread
            Data.OverreadReceivedDateTime.HasValue && // Has overread
            !Data.ProcessOverreadCommandSentDateTime.HasValue; // We have not raised a command to process the overread yet

        private bool ShouldCreateFlag =>
            _config.ShouldCreateFlags && // Enabled
            !Data.HoldReleasedDateTime.HasValue && // Hold has not been released; CDI will not accept flags after the hold is released
            Data.NeedsFlag is true && // Needs a flag
            !Data.CreateFlagCommandSentDateTime.HasValue; // We have not raised a command to create the flag yet

        private bool ShouldReleaseHold =>
            _config.ShouldReleaseHolds && // Enabled
            Data.HoldId.HasValue && // We know which hold to release
            !Data.ReleaseHoldCommandSentDateTime.HasValue && // We have not raised a command to release the hold yet
            !Data.HoldReleasedDateTime.HasValue && // Hold has not expired
            (
                Data.NeedsOverread is false // We know we don't need an overread
                ||
                Data.NeedsFlag is false // We processed an overread, and according to the results, we know we don't need a flag (or Scenario 2)
                ||
                (Data.NeedsFlag is true && Data.FlagCreatedDateTime.HasValue) // Needs a flag according to the overread, and the flag was successfully created
            );

        private bool ShouldProcessPdfDelivery =>
            Data.IsBillable.HasValue && // We know for a fact it is, or is not, billable
            HasReceivedPdfDelivery && // Client has been sent the pdf
            !Data.ProcessPdfDeliveryCommandSentDateTime.HasValue; // We have not raised a command to process the pdfdelivery yet
        #endregion Rules

        #region Tasks
        private Task EnqueueOutstandingTasks(IPipelineContext context)
        {
            var tasks = new List<Task>();

            if (ShouldProcessOverread)
                tasks.Add(ProcessOverread(context));

            if (ShouldCreateFlag)
                tasks.Add(CreateFlag(context));

            if (ShouldReleaseHold)
                tasks.Add(ReleaseHold(context));

            if (ShouldProcessPdfDelivery)
                tasks.Add(ProcessPdfDelivery(context));

            if (!tasks.Any())
                _logger.LogInformation("Nothing to do for EvaluationId={EvaluationId} at this time", Data.EvaluationId);
            else
                _logger.LogInformation("Enqueuing {TaskCount} outstanding tasks for EvaluationId={EvaluationId}", tasks.Count, Data.EvaluationId);

            return Task.WhenAll(tasks);
        }

        private async Task ProcessOverread(IPipelineContext context)
        {
            await context.SendLocal(new ProcessOverread(Data.EvaluationId, Data.OverreadResultId!.Value));

            Data.ProcessOverreadCommandSentDateTime = _applicationTime.UtcNow();
        }

        private async Task CreateFlag(IPipelineContext context)
        {
            await context.SendLocal(new CreateFlag(Data.EvaluationId));

            Data.CreateFlagCommandSentDateTime = _applicationTime.UtcNow();
        }

        private async Task ReleaseHold(IPipelineContext context)
        {
            await context.SendLocal(new ReleaseHold(Data.EvaluationId, Data.HoldId!.Value));

            Data.ReleaseHoldCommandSentDateTime = _applicationTime.UtcNow();
        }

        private async Task ProcessPdfDelivery(IPipelineContext context)
        {
            await context.SendLocal(new ProcessPdfDelivery(Data.EvaluationId, Data.PdfDeliveredToClientId!.Value, Data.IsBillable!.Value));

            Data.ProcessPdfDeliveryCommandSentDateTime = _applicationTime.UtcNow();
        }
        #endregion Tasks
    }
}
