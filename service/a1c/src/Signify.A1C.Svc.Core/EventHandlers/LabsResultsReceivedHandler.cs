using System.Threading;
using System.Threading.Tasks;
using MediatR;
using AutoMapper;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.A1C.Svc.Core.Commands;
using Signify.A1C.Svc.Core.Data.Entities;
using Signify.A1C.Svc.Core.Events;
using Signify.AkkaStreams.Kafka;
using Signify.A1C.Svc.Core.Data;
using System;

namespace Signify.A1C.Svc.Core.EventHandlers
{
    // Handle HomeAccessResultsReceivedHandler Event.
    public class LabsResultsReceivedHandler : IHandleEvent<HomeAccessResultsReceived>
    {
        private readonly ILogger<LabsResultsReceivedHandler> _logger;
        private readonly IMediator _mediator;
		private readonly IMapper _mapper;
        private const string ProductCode = "A1C";
        private readonly A1CDataContext _dataContext;

        public LabsResultsReceivedHandler(ILogger<LabsResultsReceivedHandler> logger, IMediator mediator, IMapper mapper, A1CDataContext dataContext)
        {
            _logger = logger;
            _mediator = mediator;
			_mapper = mapper;
            _dataContext = dataContext;
        }

        [Transaction]
        public async Task Handle(HomeAccessResultsReceived @event, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Start Handle HomeAccessResultsReceivedEvent, EventId: {@event.EventId}");

            //Filter product code
            var isA1CProduct = string.Equals(@event.LabTestType, ProductCode, StringComparison.OrdinalIgnoreCase);
            if (!isA1CProduct)
            {
                _logger.LogInformation($"Task Completed. LabResult Ignored, Barcode:{@event.Barcode}");
                return;
            }

            //Query database to check if HomeAccessLabResult exists.
            _logger.LogInformation($"Looking for valid A1C for OrderCorrelationID:{@event.OrderCorrelationId}");
            var a1C = await _mediator.Send(new Queries.GetA1CByCorrelation { OrderCorrelationId = @event.OrderCorrelationId }, cancellationToken);
            if (a1C == null)
            {
                throw new ApplicationException($"OrderCorrelationId does not exists in A1C DB for Barcode:{@event.Barcode}");
            }

            //Check if Lab Results already exists.
            var haLabResult = await _mediator.Send(new Queries.GetLabResults { OrderCorrelationId = @event.OrderCorrelationId }, cancellationToken);
            if (haLabResult != null)
            {
                _logger.LogInformation($"Task ignored. LabResult Barcode:{@event.Barcode} already exists in DB");
                return;
            }

            var createLabResult = _mapper.Map<CreateLabResult>(@event);
            createLabResult.A1CId = a1C.A1CId;

            await using (var transaction = await _dataContext.Database.BeginTransactionAsync(cancellationToken))
            {
                //Add LabResults 
                _logger.LogInformation($"Add Home Access Lab Results for A1CId:{a1C.A1CId}");

                await _mediator.Send(createLabResult, cancellationToken);

                //Update Status
               _logger.LogInformation($"Update Status Home Access Lab Results for A1CId:{a1C.A1CId}");
               
                if (string.IsNullOrEmpty(createLabResult.Exception))
                {
                    _logger.LogInformation($"Adding Lab Results valid status for A1CId:{a1C.A1CId}");
                    await _mediator.Send(new CreateA1CStatus
                    { A1CId = a1C.A1CId, StatusCode = A1CStatusCode.ValidLabResultsReceived }, cancellationToken);
                }
                else
                {
                    _logger.LogInformation($"Adding Lab Results invalid status for A1CId:{a1C.A1CId}");
                    await _mediator.Send(new CreateA1CStatus
                    { A1CId = a1C.A1CId, StatusCode = A1CStatusCode.InvalidLabResultsReceived }, cancellationToken);
                }
                await transaction.CommitAsync(cancellationToken);
            }

            _logger.LogInformation($"End Handle HomeAccessResultsReceivedEvent, EventId: {@event.EventId}");

        }
    }
}

