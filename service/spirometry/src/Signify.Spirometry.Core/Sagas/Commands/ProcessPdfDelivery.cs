using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Events;
using Signify.Spirometry.Core.Infrastructure;
using Signify.Spirometry.Core.Queries;
using SpiroNsb.SagaEvents;
using SpiroNsbEvents;
using System.Threading;
using System.Threading.Tasks;
using StatusCode = Signify.Spirometry.Core.Models.StatusCode;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace SpiroNsb.SagaCommands
{
    /// <summary>
    /// Command to process a pdfdelivery event. Handles either raising a BillableEvent, or
    /// tracking a BillingRequestNotSent status.
    /// </summary>
    public class ProcessPdfDelivery
    {
        public long EvaluationId { get; set; }

        public int PdfDeliveredToClientId { get; set; }

        public bool IsBillable { get; set; }

        public ProcessPdfDelivery(long evaluationId, int pdfDeliveredToClientId, bool isBillable)
        {
            EvaluationId = evaluationId;
            PdfDeliveredToClientId = pdfDeliveredToClientId;
            IsBillable = isBillable;
        }
    }

    public class ProcessPdfDeliveryHandler : IHandleMessages<ProcessPdfDelivery>
    {
        private readonly ILogger<ProcessPdfDeliveryHandler> _logger;
        private readonly IApplicationTime _applicationTime;
        private readonly ITransactionSupplier _transactionSupplier;
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;

        public ProcessPdfDeliveryHandler(ILogger<ProcessPdfDeliveryHandler> logger,
            IApplicationTime applicationTime,
            ITransactionSupplier transactionSupplier,
            IMapper mapper,
            IMediator mediator)
        {
            _logger = logger;
            _applicationTime = applicationTime;
            _transactionSupplier = transactionSupplier;
            _mapper = mapper;
            _mediator = mediator;
        }

        [Transaction]
        public async Task Handle(ProcessPdfDelivery message, IMessageHandlerContext context)
        {
            _logger.LogInformation("Processing PdfDelivery for EvaluationId={EvaluationId}, PdfDeliveredToClientId={PdfDeliveredToClientId}, IsBillable={IsBillable}",
                message.EvaluationId, message.PdfDeliveredToClientId, message.IsBillable);

            var exam = await _mediator.Send(new QuerySpirometryExam(message.EvaluationId), context.CancellationToken);

            var pdfEntity = (await _mediator.Send(new QueryPdfDeliveredToClient(message.EvaluationId), context.CancellationToken)).Entity;

            using var transaction = _transactionSupplier.BeginTransaction();

            await SendStatus(pdfEntity, exam, StatusCode.ClientPdfDelivered, context.CancellationToken);

            if (!message.IsBillable)
            {
                _logger.LogInformation("Not sending a billing request for EventId={EventId}, because this EvaluationId={EvaluationId} is not billable", pdfEntity.EventId, message.EvaluationId);
                await SendStatus(pdfEntity, exam, StatusCode.BillRequestNotSent, context.CancellationToken);
                await Complete(transaction);
                return;
            }

            var billRequestSent = (await _mediator.Send(new QueryBillRequestSent(message.EvaluationId), context.CancellationToken)).Entity;
            if (billRequestSent != null)
            {
                // No matter if one or more PDFs are delivered to the client for an evaluation, we can only bill for a performed Spirometry exam once

                _logger.LogInformation("Already sent a billing request for EvaluationId={EvaluationId}, nothing left to do for EventId={EventId}", message.EvaluationId, pdfEntity.EventId);
                await Complete(transaction);
                return;
            }

            // Raise a billable event
            var billableEvent = _mapper.Map<BillableEvent>(pdfEntity);

            await context.SendLocal(billableEvent);

            _logger.LogInformation("Billable event raised for EvaluationId={EvaluationId}, EventId={EventId}", message.EvaluationId, pdfEntity.EventId);

            await Complete(transaction);

            async Task Complete(IBufferedTransaction tran)
            {
                await context.SendLocal(new PdfDeliveryProcessedEvent
                {
                    EvaluationId = message.EvaluationId,
                    CreatedDateTime = _applicationTime.UtcNow()
                });

                await tran.CommitAsync(context.CancellationToken);
            }
        }

        private Task SendStatus(PdfDeliveredToClient message, SpirometryExam exam, StatusCode statusCode, CancellationToken token)
        {
            return _mediator.Send(new ExamStatusEvent
            {
                EventId = message.EventId,
                Exam = exam,
                StatusCode = statusCode,
                StatusDateTime = message.DeliveryDateTime
            }, token);
        }
    }
}
