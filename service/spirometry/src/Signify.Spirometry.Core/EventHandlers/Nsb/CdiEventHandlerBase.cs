using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.Dps.Observability.Library.Services;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Events;
using Signify.Spirometry.Core.Infrastructure;
using SpiroNsb.SagaEvents;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public abstract class CdiEventHandlerBase
{
    private ILogger Logger { get; }
    private IMediator Mediator { get; }
    private IMapper Mapper { get; }
    private IApplicationTime ApplicationTime { get; }
    private ITransactionSupplier TransactionSupplier { get; }
    private IPublishObservability PublishObservability { get; }

    protected CdiEventHandlerBase(ILogger logger, IMediator mediator, IMapper mapper, IApplicationTime applicationTime,
        ITransactionSupplier transactionSupplier, IPublishObservability publishObservability)
    {
        Logger = logger;
        Mediator = mediator;
        Mapper = mapper;
        ApplicationTime = applicationTime;
        TransactionSupplier = transactionSupplier;
        PublishObservability = publishObservability;
    }

    [Transaction]
    protected async Task Handle(CdiEventBase message, IMessageHandlerContext context)
    {
        Logger.LogInformation("Handling CDI event for {Event}; EvaluationId={EvaluationId}, RequestId={RequestId}",
            message.GetType().Name, message.EvaluationId, message.RequestId);
        using var transaction = TransactionSupplier.BeginTransaction();
        var cdiEvent = Mapper.Map<CdiEventForPayment>(message);
        var cdiEventDetails = await Mediator.Send(new AddCdiEventForPayment(cdiEvent), context.CancellationToken);
        if (!cdiEventDetails.IsNew)
        {
            Logger.LogDebug("{Event} already handled for EvaluationId={EvaluationId}, RequestId={RequestId}",
                message.GetType().Name, message.EvaluationId, message.RequestId);
            await CommitTransactions(transaction, context.CancellationToken);
            return;
        }

        var paymentSaga = Mapper.Map<CdiEventForPaymentReceived>(cdiEventDetails.CdiEventForPayment);
        await context.SendLocal(paymentSaga);
        await CommitTransactions(transaction, context.CancellationToken);
    }

    /// <summary>
    /// Commit both TransactionSupplier and Observability transactions
    /// </summary>
    /// <param name="transaction">The transaction generated as part of TransactionSupplier.BeginTransaction</param>
    /// <param name="token"></param>
    [Trace]
    private async Task CommitTransactions(IBufferedTransaction transaction, CancellationToken token)
    {
        await transaction.CommitAsync(token);
        PublishObservability.Commit();
    }
}