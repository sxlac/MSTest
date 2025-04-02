using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.DEE.Svc.Core.ApiClient;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Events;
using Signify.DEE.Svc.Core.Infrastructure;
using System.Threading.Tasks;
using Signify.Dps.Observability.Library.Services;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public class CdiPassedEventHandler(
    ILogger<CdiPassedEventHandler> logger,
    ITransactionSupplier transactionSupplier,
    IMediator mediator,
    IMapper mapper,
    IPublishObservability publishObservability,
    IEvaluationApi evaluationApi,
    IApplicationTime applicationTime)
    : CdiEventHandlerBase(logger, mediator, mapper, transactionSupplier, publishObservability, evaluationApi,
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