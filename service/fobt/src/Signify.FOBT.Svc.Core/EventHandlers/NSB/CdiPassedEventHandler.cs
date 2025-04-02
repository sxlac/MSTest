using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.FOBT.Svc.Core.ApiClient;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Infrastructure;
using Signify.FOBT.Svc.Core.Infrastructure.Observability;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public class CdiPassedEventHandler : CdiEventHandlerBase, IHandleMessages<CDIPassedEvent>
{
    public CdiPassedEventHandler(ILogger<CdiPassedEventHandler> logger,
        ITransactionSupplier transactionSupplier,
        IMediator mediator,
        IMapper mapper,
        IPublishObservability publishObservability,
        IEvaluationApi evaluationApi,
        IApplicationTime applicationTime)
        : base(logger, mediator, mapper, transactionSupplier, publishObservability, evaluationApi, applicationTime)
    {
    }

    [Transaction]
    public async Task Handle(CDIPassedEvent message, IMessageHandlerContext context)
    {
        Logger.LogInformation("Handling CDI event for CDIPassedEvent; EvaluationId={EvaluationId}, RequestId={RequestId}",
            message.EvaluationId, message.RequestId);

        await base.Handle(message, FOBTStatusCode.CdiPassedReceived, context);

        Logger.LogInformation("Finished handling CDI event for CDIPassedEvent; EvaluationId={EvaluationId}, RequestId={RequestId}",
            message.EvaluationId, message.RequestId);
    }
}