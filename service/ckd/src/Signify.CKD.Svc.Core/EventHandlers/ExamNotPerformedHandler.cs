using System.Collections.Generic;
using MediatR;
using NServiceBus;
using Signify.CKD.Svc.Core.Commands;
using Signify.CKD.Svc.Core.Events;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Signify.CKD.Svc.Core.Constants;
using Signify.CKD.Svc.Core.Infrastructure.Observability;

namespace Signify.CKD.Svc.Core.EventHandlers
{
    public class ExamNotPerformedHandler : IHandleMessages<ExamNotPerformedEvent>
    {
        private readonly IMediator _mediator;
        private readonly IObservabilityService _observabilityService;
        private readonly ILogger<ExamNotPerformedHandler> _logger;
        
        public ExamNotPerformedHandler(ILogger<ExamNotPerformedHandler> logger, IMediator mediator, 
            IObservabilityService observabilityService)
        {
            _logger = logger;
            _mediator = mediator;
            _observabilityService = observabilityService;
        }

        public async Task Handle(ExamNotPerformedEvent message, IMessageHandlerContext context)
        {
            await _mediator.Send(new AddExamNotPerformed(message.Exam, message.NotPerformedReasonId, message.NotPerformedReasonNotes));
            
            _logger.LogInformation("Finished process not performed event for EvaluationId={EvaluationId}", message.Exam.EvaluationId);
            
            _observabilityService.AddEvent(Observability.Evaluation.EvaluationNotPerformedEvent, new Dictionary<string, object>()
            {
                {Observability.EventParams.EvaluationId, message.Exam.EvaluationId},
                {Observability.EventParams.CreatedDateTime, message.Exam.CreatedDateTime.ToUnixTimeSeconds()}
            });
            
            // Future story - Publish to Status Kafka topic. Ensure AddDashboardEvent remains at the end of workflow when implemented
        }
    }
}
