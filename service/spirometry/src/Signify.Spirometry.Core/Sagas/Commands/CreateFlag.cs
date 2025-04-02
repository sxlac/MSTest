using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.Spirometry.Core.Configs.Loopback;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Events;
using Signify.Spirometry.Core.Exceptions;
using Signify.Spirometry.Core.Queries;
using SpiroNsb.SagaEvents;
using System.Threading;
using System.Threading.Tasks;

using CreateFlagCommand = Signify.Spirometry.Core.Commands.CreateFlag;
using StatusCodeModel = Signify.Spirometry.Core.Models.StatusCode;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace SpiroNsb.SagaCommands
{
    /// <summary>
    /// Command to create a flag in CDI to send a query to a provider in a clarification
    /// </summary>
    public class CreateFlag : ISagaCommand
    {
        /// <inheritdoc />
        public long EvaluationId { get; set; }

        public CreateFlag(long evaluationId)
        {
            EvaluationId = evaluationId;
        }
    }

    public class CreateFlagHandler : IHandleMessages<CreateFlag>
    {
        private readonly ILogger _logger;
        private readonly IGetLoopbackConfig _config;
        private readonly ITransactionSupplier _transactionSupplier;
        private readonly IMediator _mediator;

        public CreateFlagHandler(ILogger<CreateFlagHandler> logger,
            IGetLoopbackConfig config,
            ITransactionSupplier transactionSupplier,
            IMediator mediator)
        {
            _logger = logger;
            _config = config;
            _transactionSupplier = transactionSupplier;
            _mediator = mediator;
        }

        [Transaction]
        public async Task Handle(CreateFlag message, IMessageHandlerContext context)
        {
            _logger.LogInformation("Received CreateFlag request for EvaluationId={EvaluationId}", message.EvaluationId);

            if (!_config.ShouldCreateFlags)
            {
                _logger.LogInformation("Creation of flags is disabled, not processing for EvaluationId={EvaluationId}",
                    message.EvaluationId);

                // Throw an exception instead of just `return` so the message isn't permanently lost and
                // can be replayed from the error queue if needed.
                throw new FeatureDisabledException(message.EvaluationId, nameof(_config.ShouldCreateFlags));
            }

            using var transaction = _transactionSupplier.BeginTransaction();

            if (await IsHoldReleased(message, context.CancellationToken))
            {
                _logger.LogInformation("The hold for EvaluationId={EvaluationId} is already released or expired, cannot create a clarification flag for it", message.EvaluationId);
                return;
            }

            var (exam, results, flag) = await GetData(message, context.CancellationToken);

            if (flag == null)
            {
                flag = await _mediator.Send(new CreateFlagCommand
                {
                    Exam = exam,
                    Results = results
                }, context.CancellationToken);

                await SaveFlagCreatedStatus(exam, flag, context.CancellationToken);
            }
            else
            {
                _logger.LogInformation("Flag already exists for EvaluationId={EvaluationId}, ClarificationFlagId={ClarificationFlagId}", message.EvaluationId, flag.ClarificationFlagId);
            }

            await context.SendLocal(new FlagCreatedEvent(message.EvaluationId, flag.CreateDateTime, flag.ClarificationFlagId));

            await transaction.CommitAsync(context.CancellationToken);
        }

        private async Task<bool> IsHoldReleased(ISagaCommand request, CancellationToken token)
        {
            var hold = await _mediator.Send(new QueryHold
            {
                EvaluationId = request.EvaluationId
            }, token);

            return hold is {ReleasedDateTime: { }}; // hold and released date not null
        }

        private async Task<(SpirometryExam exam, SpirometryExamResult results, ClarificationFlag flag)> GetData(ISagaCommand request, CancellationToken token)
        {
            var exam = await _mediator.Send(new QuerySpirometryExam(request.EvaluationId)
            {
                IncludeResults = true, // join, to save db hits
                IncludeClarificationFlag = true
            }, token);

            return (exam, exam.SpirometryExamResult, exam.ClarificationFlag);
        }

        private Task SaveFlagCreatedStatus(SpirometryExam exam, ClarificationFlag flag, CancellationToken token)
        {
            return _mediator.Send(new ExamStatusEvent
            {
                Exam = exam,
                StatusCode = StatusCodeModel.ClarificationFlagCreated,
                StatusDateTime = flag.CreateDateTime
            }, token);
        }
    }
}
