using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Events.Akka;
using System.Threading.Tasks;
using Signify.Dps.Observability.Library.Services;
using Signify.eGFR.Core.ApiClients.EvaluationApi;
using Signify.eGFR.Core.Infrastructure;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public class CdiPassedEventHandler(
    ILogger<CdiPassedEventHandler> logger,
    IMediator mediator,
    ITransactionSupplier transactionSupplier,
    IPublishObservability publishObservability,
    IApplicationTime applicationTime,
    IMapper mapper,
    IEvaluationApi evaluationApi)
    : CdiEventHandlerBase(logger, mediator, transactionSupplier, publishObservability, applicationTime, mapper, evaluationApi), IHandleMessages<CDIPassedEvent>
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