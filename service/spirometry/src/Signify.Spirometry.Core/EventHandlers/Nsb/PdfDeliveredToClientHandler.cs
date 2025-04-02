using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Constants;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Infrastructure;
using Signify.Spirometry.Core.Queries;
using SpiroEvents;
using SpiroNsb.SagaEvents;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using PdfEntity = Signify.Spirometry.Core.Data.Entities.PdfDeliveredToClient;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers
{
    /// <summary>
    /// NSB event handler for the <see cref="PdfDeliveredToClient"/>
    /// </summary>
    public class PdfDeliveredToClientHandler : IHandleMessages<PdfDeliveredToClient>
    {
        private readonly ILogger _logger;
        private readonly IMediator _mediator;
        private readonly ITransactionSupplier _transactionSupplier;
        private readonly IApplicationTime _applicationTime;
        private readonly IPublishObservability _publishObservability;
        
        public PdfDeliveredToClientHandler(ILogger<PdfDeliveredToClientHandler> logger,
            IMediator mediator,
            ITransactionSupplier transactionSupplier,
            IApplicationTime applicationTime,
            IPublishObservability publishObservability)
        {
            _logger = logger;
            _mediator = mediator;
            _transactionSupplier = transactionSupplier;
            _applicationTime = applicationTime;
            _publishObservability = publishObservability;
        }

        [Transaction]
        public async Task Handle(PdfDeliveredToClient message, IMessageHandlerContext context)
        {
            if (await HasPdfDeliveryEvent(message.EvaluationId, context.CancellationToken))
            {
                _logger.LogInformation("Already processed a PdfDeliveredToClient event for EvaluationId={EvaluationId}, ignoring EventId={EventId}", message.EvaluationId, message.EventId);
                return;
            }

            _logger.LogInformation("Received PdfDeliveredToClient with EventId={EventId}, for EvaluationId={EvaluationId}", message.EventId, message.EvaluationId);

            using var transaction = _transactionSupplier.BeginTransaction();

            // Save record to db
            var pdfEntity = await _mediator.Send(new AddPdfDeliveredToClient(message), context.CancellationToken);

            await SendPdfDeliveryEvent(pdfEntity, context);

            await transaction.CommitAsync(context.CancellationToken);
            
            PublishObservability(message, Observability.PdfDelivered.PdfDeliveryReceivedEvent);
        }

        private async Task<bool> HasPdfDeliveryEvent(long evaluationId, CancellationToken token)
        {
            var result = await _mediator.Send(new QueryPdfDeliveredToClient(evaluationId), token);

            return result.Entity != null;
        }

        private Task SendPdfDeliveryEvent(PdfEntity entity, IPipelineContext context)
        {
            return context.SendLocal(new PdfDeliveredToClientEvent
            {
                EvaluationId = entity.EvaluationId,
                PdfDeliveredToClientId = entity.PdfDeliveredToClientId,
                CreatedDateTime = _applicationTime.UtcNow()
            });
        }
        
        private void PublishObservability(PdfDeliveredToClient message, string eventType)
        {
            var observabilityPdfDeliveryReceivedEvent = new ObservabilityEvent
            {
                EvaluationId = (int) message.EvaluationId,
                EventType = eventType,
                EventValue = new Dictionary<string, object>
                {
                    { Observability.EventParams.EvaluationId, message.EvaluationId },
                    { Observability.EventParams.CreatedDateTime, ((DateTimeOffset)message.CreatedDateTime).ToUnixTimeSeconds() }
                }
            };

            _publishObservability.RegisterEvent(observabilityPdfDeliveryReceivedEvent, true);
        }
    }
}
