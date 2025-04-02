using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.HBA1CPOC.Messages.Events;
using System.Threading.Tasks;

namespace Signify.HBA1CPOC.Svc.Core.EventHandlers;

/// <summary>
///This handles HBA1CPOC Performed event. 
/// </summary>
public class A1CPOCPerformedHandler : IHandleMessages<A1CPOCPerformed>
{
    private readonly ILogger<A1CPOCPerformedHandler> _logger;

    public A1CPOCPerformedHandler(ILogger<A1CPOCPerformedHandler> logger)
    {
        _logger = logger;
    }

    [Transaction]
    public Task Handle(A1CPOCPerformed message, IMessageHandlerContext context)
    {
        _logger.LogDebug("HBA1CPOCPerformed Handler received Evaluation with EvaluationID={EvaluationId}", message.EvaluationId);

        return Task.CompletedTask;
    }
}