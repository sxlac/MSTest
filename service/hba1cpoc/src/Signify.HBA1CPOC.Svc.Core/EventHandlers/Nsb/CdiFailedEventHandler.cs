using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.HBA1CPOC.Messages.Events.Akka;
using Signify.HBA1CPOC.Svc.Core.ApiClient;
using Signify.HBA1CPOC.Svc.Core.BusinessRules;
using Signify.HBA1CPOC.Svc.Core.Data;
using Signify.HBA1CPOC.Svc.Core.Data.Entities;
using Signify.HBA1CPOC.Svc.Core.Infrastructure;
using Signify.HBA1CPOC.Svc.Core.Infrastructure.Observability;

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
        IPayableRules payableRules,
        IPublishObservability publishObservability,
        IEvaluationApi evaluationApi, 
        IApplicationTime applicationTime)
        : base(logger, mediator, mapper, payableRules, publishObservability, evaluationApi, applicationTime)
    {
        _transactionSupplier = transactionSupplier;
    }

    [Transaction]
    public async Task Handle(CDIFailedEvent message, IMessageHandlerContext context)
    {
        Logger.LogInformation("Handling CDI event for EvaluationId={EvaluationId}, RequestId={RequestId}, with PayProvider={PayProvider}",
            message.EvaluationId, message.RequestId, message.PayProvider);

        var exam = await GetExam(message);
        if (exam is null)
        {
            await HandleNullExam(message.EvaluationId, message.RequestId);
            return;
        }
        if (!await IsPerformed(exam))
        {
            Logger.LogInformation("Nothing to do, exam was not performed, for EvaluationId={EvaluationId}, RequestId={RequestId}, with PayProvider={PayProvider}",
                message.EvaluationId, message.RequestId, message.PayProvider);
            return;
        }

        var status = message.PayProvider ? HBA1CPOCStatusCode.CdiFailedWithPayReceived : HBA1CPOCStatusCode.CdiFailedWithoutPayReceived;

        using var transaction = _transactionSupplier.BeginTransaction();

        await PublishStatus(context, message, status);

        if (message.PayProvider)
        {
            await base.Handle(message, exam, context);
        }
        else
        {
            await PublishStatus(context, message, HBA1CPOCStatusCode.ProviderNonPayableEventReceived, "PayProvider is false for the CDIFailedEvent");
        }

        await transaction.CommitAsync(context.CancellationToken);

        Logger.LogInformation("Finished handling CDI event for EvaluationId={EvaluationId}, RequestId={RequestId}, with PayProvider={PayProvider}",
            message.EvaluationId, message.RequestId, message.PayProvider);
    }
}
