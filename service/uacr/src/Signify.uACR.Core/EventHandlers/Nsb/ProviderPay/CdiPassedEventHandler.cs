using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.Dps.Observability.Library.Services;
using Signify.uACR.Core.ApiClients.EvaluationApi;
using Signify.uACR.Core.Data;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.EventHandlers.Nsb;
using Signify.uACR.Core.Events.Akka;
using Signify.uACR.Core.Infrastructure;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public class CdiPassedEventHandler(
    ILogger<CdiPassedEventHandler> logger,
    IMediator mediator,
    IMapper mapper,
    ITransactionSupplier transactionSupplier,
    IEvaluationApi evaluationApi,
    IPublishObservability publishObservability,
    IApplicationTime applicationTime)
    : CdiEventHandlerBase(logger, mediator, mapper, transactionSupplier, evaluationApi, publishObservability,
        applicationTime), IHandleMessages<CDIPassedEvent>
{
    [Transaction]
    public async Task Handle(CDIPassedEvent message, IMessageHandlerContext context)
    {
        Logger.LogInformation("Handling CDI event for CDIPassedEvent; EvaluationId={EvaluationId}, RequestId={RequestId}",
            message.EvaluationId, message.RequestId);

        await base.Handle(message, ExamStatusCode.CdiPassedReceived, context);

        Logger.LogInformation("Finished handling CDI event for CDIPassedEvent; EvaluationId={EvaluationId}, RequestId={RequestId}",
            message.EvaluationId, message.RequestId);
    }
}