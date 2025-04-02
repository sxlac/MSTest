using System.Collections.Generic;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.CKD.Messages.Events;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.Data;
using System.Threading.Tasks;
using Signify.CKD.Svc.Core.Constants;
using Signify.CKD.Svc.Core.Infrastructure.Observability;

namespace Signify.CKD.Svc.Core.EventHandlers
{
    public class DateOfServiceUpdateHandler : IHandleMessages<DateOfServiceUpdated>
    {
        private readonly ILogger<DateOfServiceUpdateHandler> _logger;
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly CKDDataContext _dataContext;
        private readonly IObservabilityService _observabilityService;
        
        public DateOfServiceUpdateHandler(ILogger<DateOfServiceUpdateHandler> logger,
            IMediator mediator,
            CKDDataContext dataContext,
            IMapper mapper,
            IObservabilityService observabilityService)
        {
            _logger = logger;
            _mediator = mediator;
            _dataContext = dataContext;
            _mapper = mapper;
            _observabilityService = observabilityService;
        }

        [Transaction]
        public async Task Handle(DateOfServiceUpdated message, IMessageHandlerContext context)
        {
            //Guaranteed that this evaluationId exists in DB, otherwise NSB would not have published
            var ckd = await _dataContext.CKD.AsNoTracking().FirstOrDefaultAsync(s => s.EvaluationId == message.EvaluationId);
            var oldDos = ckd.DateOfService;
            ckd.DateOfService = message.DateOfService;
            var createOrUpdateCkd = _mapper.Map<CreateOrUpdateCKD>(ckd);

            await _mediator.Send(createOrUpdateCkd);
            
            _logger.LogInformation("DateOfService updated from {oldDos} to {newDos} for {EvaluationId}", oldDos, message.DateOfService, message.EvaluationId);
            
            _observabilityService.AddEvent(Observability.Evaluation.EvaluationDosUpdatedEvent, new Dictionary<string, object>
            {
                {Observability.EventParams.EvaluationId, ckd.EvaluationId},
                {Observability.EventParams.CreatedDateTime, ckd.CreatedDateTime.ToUnixTimeSeconds()}
            });
        }
    }
}
