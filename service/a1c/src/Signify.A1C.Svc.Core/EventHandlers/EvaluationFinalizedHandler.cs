using System;
using System.Linq;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Signify.A1C.Svc.Core.Events;
using Signify.AkkaStreams.Kafka;
using System.Threading;
using System.Threading.Tasks;
using NewRelic.Api.Agent;
using Signify.A1C.Messages.Events;

namespace Signify.A1C.Svc.Core.EventHandlers
{
    /// <summary>
    ///This handles evaluation finalized event. It filters A1C products and raise A1C Received Event.
    /// </summary>
    public class EvaluationFinalizedHandler : IHandleEvent<EvaluationFinalizedEvent>
    {
        private readonly IEndpointInstance _endpoint;
        private readonly ILogger<EvaluationFinalizedHandler> _logger;
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private const string ProductCode = "HBA1C";

        public EvaluationFinalizedHandler(ILogger<EvaluationFinalizedHandler> logger, IMediator mediator,
            IEndpointInstance endpoint, IMapper mapper)
        {
            _logger = logger;
            _endpoint = endpoint;
            _mediator = mediator;
            _mapper = mapper;

        }

        [Transaction]
        public async Task Handle(EvaluationFinalizedEvent @event, CancellationToken cancellationToken)
        {
            _logger.LogDebug($"Start Handle EvaluationFinalized, EvaluationID: {@event.EvaluationId}, EventId: {@event.Id}");

            //Filter product code
            var isA1CProduct = @event.Products.Any(p =>
                string.Equals(p.ProductCode, ProductCode, StringComparison.OrdinalIgnoreCase));
            if (!isA1CProduct)
            {
                _logger.LogDebug($"Task Completed. Evaluation Ignored, EvaluationID:{@event.EvaluationId}");
                return;
            }

            _logger.LogInformation(
                $"Evaluation Identified with product code A1C, EvaluationID : {@event.EvaluationId}, EventId: {@event.Id}");

            //Query database to check if A1C exists.
            var a1C = await _mediator.Send(new Queries.GetA1C { EvaluationId = @event.EvaluationId }, cancellationToken);
            if (a1C != null) 
            {
                await UpdateDateOfService(@event.DateOfService, a1C, @event.EvaluationId, @event.Id);
                return;
            }
            bool performed = true;
            //Query Evaluation api and filter eval answers
            var barcode = await _mediator.Send(new Queries.CheckA1CEval { EvaluationId = @event.EvaluationId },
                cancellationToken);
            if (string.IsNullOrEmpty(barcode))
            {
                _logger.LogInformation(
                    $"Task Completed. A1C not delivered or No barcode found. EvaluationID: {@event.EvaluationId}, EventId: {@event.Id}");
                performed = false;
            }

            //Raise A1C Evaluation Received NService bus event
            var evalReceived = _mapper.Map<A1CEvaluationReceived>(@event);
            evalReceived.Barcode = barcode;
            evalReceived.Performed = performed;

            await _endpoint.Publish(evalReceived);

            _logger.LogDebug("End Handle EvaluationFinalized");
        }

        internal async Task UpdateDateOfService(DateTime? eventDos, Data.Entities.A1C a1C,
                                                    int evaluationId, Guid eventId)
        {
            if (Nullable.Compare(eventDos, a1C.DateOfService) == 0)
            {
                _logger.LogDebug(
                    $"Task Completed. Already processed Eval and no change in DOS. EvaluationID: {evaluationId}, EventId: {eventId}");
            }
            else //save updated DOS
            {
                if (eventDos == null)
                {
                    _logger.LogInformation(
                        $"Evaluation exists, DOS is null and no action taken, EvaluationID : {evaluationId}, EventId: {eventId}");
                }
                else
                {
                    var dosUpdate = new DateOfServiceUpdated(evaluationId, eventDos.Value);
                    await _endpoint.Send(dosUpdate);
                    _logger.LogInformation(
                        $"Evaluation exists and DosUpdate event published, EvaluationID : {evaluationId}, EventId: {eventId}");
                }
            }

            _ = Task.CompletedTask;
        }
    }
}
