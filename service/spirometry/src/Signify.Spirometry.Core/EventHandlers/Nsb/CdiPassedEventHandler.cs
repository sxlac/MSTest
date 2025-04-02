using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.Dps.Observability.Library.Services;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Events;
using Signify.Spirometry.Core.Infrastructure;

// ReSharper disable once CheckNamespace
// AzureServiceBus has hard limit of 50 characters for classnames on published events, including namespace, so we use
// a much-shortened namespace name to allow our event classes to remain verbosely named.
namespace NsbEventHandlers;

public class CdiPassedEventHandler : CdiEventHandlerBase, IHandleMessages<CDIPassedEvent>
{
    public CdiPassedEventHandler(ILogger<CdiPassedEventHandler> logger, IMediator mediator, IMapper mapper, IApplicationTime applicationTime,
        ITransactionSupplier transactionSupplier, IPublishObservability publishObservability)
        : base(logger, mediator, mapper, applicationTime, transactionSupplier, publishObservability)
    {
    }

    [Transaction]
    public async Task Handle(CDIPassedEvent message, IMessageHandlerContext context)
    {
        await base.Handle(message, context);
    }
}