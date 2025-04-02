using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.CKD.Svc.Core.BusinessRules;
using Signify.CKD.Svc.Core.Data;
using Signify.CKD.Svc.Core.Data.Entities;
using Signify.CKD.Svc.Core.Events;
using Signify.CKD.Svc.Core.Infrastructure.Observability;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public class CdiPassedEventHandler : CdiEventHandlerBase, IHandleMessages<CDIPassedEvent>
{
    private readonly ITransactionSupplier _transactionSupplier;

    public CdiPassedEventHandler(ILogger<CdiPassedEventHandler> logger,
        IMediator mediator,
        IMapper mapper,
        IPublishObservability publishObservability,
        ITransactionSupplier transactionSupplier,
        IPayableRules payableRules)
        : base(logger, mediator, mapper, publishObservability, payableRules)
    {
        _transactionSupplier = transactionSupplier;
    }

    [Transaction]
    public async Task Handle(CDIPassedEvent message, IMessageHandlerContext context)
    {
        Logger.LogInformation("Handling CDI event for EvaluationId={EvaluationId}, RequestId={RequestId}",
            message.EvaluationId, message.RequestId);

        var exam = await GetExam(message);
        if (!await IsPerformed(exam))
        {
            Logger.LogInformation("Nothing to do, exam was not performed, for EvaluationId={EvaluationId}, RequestId={RequestId}",
                message.EvaluationId, message.RequestId);
            return;
        }

        using var transaction = _transactionSupplier.BeginTransaction();

        await PublishStatus(context, message, CKDStatusCode.CdiPassedReceived);

        await base.Handle(message, exam, context);

        await transaction.CommitAsync();

        Logger.LogInformation("Finished handling CDI event for EvaluationId={EvaluationId}, RequestId={RequestId}",
            message.EvaluationId, message.RequestId);
    }
}
