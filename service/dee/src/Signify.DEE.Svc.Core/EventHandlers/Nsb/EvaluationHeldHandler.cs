using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Events;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Queries;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.EventHandlers.Nsb
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
            var response = await _mediator.Send(new CreateHold(holdMapped), context.CancellationToken);

            if (!response.IsNew) // Sometimes CDI sends duplicate hold events, ignore them
            {
                _logger.LogInformation("Hold for EvaluationId={EvaluationId} was already received, nothing left to do", message.EvaluationId);
                return;
            }

            // If we receive the hold after we have already processed a not performed or we already have results
            // Then we'll release the hold right away.
            var exam = await _mediator.Send(new GetExamByEvaluation() { EvaluationId = message.EvaluationId }, context.CancellationToken);
            if (exam != null)
            {
                ExamResult examResults = null;
                var notPerformed = await _mediator.Send(new GetNotPerformedExamByExamId() { ExamId = exam.ExamId }, context.CancellationToken);
                if (notPerformed is null)
                {
                    examResults = await _mediator.Send(new GetExamResultByExamId() { ExamId = exam.ExamId }, context.CancellationToken);
                }
                if (notPerformed is not null || examResults is not null)
                {
                    _logger.LogInformation("Releasing hold for EvaluationId={EvaluationId}. We already have results or a not performed.", message.EvaluationId);
                    await context.SendLocal(new ReleaseHold(holdMapped));
                }
            }

            _logger.LogInformation("Finished handling evaluation held event for EvaluationId={EvaluationId} and HoldId={HoldId}",
                response.Hold.EvaluationId, response.Hold.HoldId);

            await transaction.CommitAsync(context.CancellationToken);
        }
    }
}
