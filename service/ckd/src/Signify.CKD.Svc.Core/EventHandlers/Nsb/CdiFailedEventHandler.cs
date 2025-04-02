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

public class CdiFailedEventHandler : CdiEventHandlerBase, IHandleMessages<CDIFailedEvent>
{
    private readonly ITransactionSupplier _transactionSupplier;

    public CdiFailedEventHandler(ILogger<CdiFailedEventHandler> logger,
        ITransactionSupplier transactionSupplier,
        IMediator mediator,
        IMapper mapper,
        IPublishObservability publishObservability,
        IPayableRules payableRules)
        : base(logger, mediator, mapper, publishObservability, payableRules)
    {
        _transactionSupplier = transactionSupplier;
    }

    [Transaction]
    public async Task Handle(CDIFailedEvent message, IMessageHandlerContext context)
    {
        Logger.LogInformation("Handling CDI event for EvaluationId={EvaluationId}, RequestId={RequestId}, with PayProvider={PayProvider}",
            message.EvaluationId, message.RequestId, message.PayProvider);

        var exam = await GetExam(message);
        if (!await IsPerformed(exam))
        {
            Logger.LogInformation("Nothing to do, exam was not performed, for EvaluationId={EvaluationId}, RequestId={RequestId}, with PayProvider={PayProvider}",
                message.EvaluationId, message.RequestId, message.PayProvider);
            return;
        }

        var status = message.PayProvider ? CKDStatusCode.CdiFailedWithPayReceived : CKDStatusCode.CdiFailedWithoutPayReceived;

        using var transaction = _transactionSupplier.BeginTransaction();

        await PublishStatus(context, message, status);

        if (message.PayProvider)
        {
            await base.Handle(message, exam, context);
        }
        else
        {
            await PublishStatus(context, message, CKDStatusCode.ProviderNonPayableEventReceived, "PayProvider is false for the CDIFailedEvent");
        }

        await transaction.CommitAsync();

        Logger.LogInformation("Finished handling CDI event for EvaluationId={EvaluationId}, RequestId={RequestId}, with PayProvider={PayProvider}",
            message.EvaluationId, message.RequestId, message.PayProvider);
    }
}
