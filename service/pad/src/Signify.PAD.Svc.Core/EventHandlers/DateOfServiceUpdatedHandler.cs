using System;
using System.Collections.Generic;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.Events;
using System.Threading.Tasks;
using Signify.Dps.Observability.Library.Events;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Svc.Core.Constants;

namespace Signify.PAD.Svc.Core.EventHandlers
{
    public class DateOfServiceUpdatedHandler
    {
        /// <summary>
        /// This handles DOS update of existing PAD and saves to database
        /// </summary>
        public class DateOfServiceUpdateHandler : IHandleMessages<DateOfServiceUpdated>
        {
            private readonly ILogger<DateOfServiceUpdateHandler> _logger;
            private readonly IMediator _mediator;
            private readonly IMapper _mapper;
            private readonly PADDataContext _dataContext;
            private readonly ITransactionSupplier _transactionSupplier;
            private readonly IPublishObservability _publishObservability;

            public DateOfServiceUpdateHandler(
                ILogger<DateOfServiceUpdateHandler> logger, 
                IMediator mediator,
                PADDataContext dataContext, 
                IMapper mapper, 
                ITransactionSupplier transactionSupplier,
                IPublishObservability publishObservability)
            {
                _logger = logger;
                _mediator = mediator;
                _mapper = mapper;
                _dataContext = dataContext;
                _transactionSupplier = transactionSupplier;
                _publishObservability = publishObservability;
            }

            [Transaction]
            public async Task Handle(DateOfServiceUpdated message, IMessageHandlerContext context)
            {
                //Guaranteed that this evaluationId exists in DB, otherwise NSB would not have published
                using var transaction = _transactionSupplier.BeginTransaction();

                var pad = await _dataContext.PAD.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.EvaluationId == message.EvaluationId, context.CancellationToken);
                var oldDos = pad.DateOfService;
                pad.DateOfService = message.DateOfService;

                var updatePad = _mapper.Map<CreateOrUpdatePAD>(pad);
                await _mediator.Send(updatePad, context.CancellationToken);

                await transaction.CommitAsync(context.CancellationToken);

                _logger.LogInformation(
                    "DOS updated for existing Evaluation, EvaluationID : {EvaluationId}, previous DOS: {OldDos},  new DOS: {DateOfService}",
                    message.EvaluationId, oldDos, message.DateOfService);
                
                PublishObservability(message.EvaluationId, ((DateTimeOffset)message.DateOfService).ToUnixTimeSeconds(), Observability.Evaluation.EvaluationDosUpdatedEvent, sendImmediate: true);
            }
            
            private void PublishObservability(int evaluationId, long createdDateTime, string eventType, bool sendImmediate = false)
            {
                var observabilityDosUpdatedEvent = new ObservabilityEvent
                {
                    EvaluationId = evaluationId,
                    EventType = eventType,
                    EventValue = new Dictionary<string, object>
                    {
                        {Observability.EventParams.EvaluationId, evaluationId},
                        {Observability.EventParams.CreatedDateTime, createdDateTime}
                    }
                };

                _publishObservability.RegisterEvent(observabilityDosUpdatedEvent, sendImmediate);
            }
        }
    }
}