using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.eGFR.Core.Events;
using Signify.eGFR.Core.Infrastructure;

namespace Signify.eGFR.Core.EventHandlers.Akka;

/// <summary>
/// Akka/Kafka event handler for the <see cref="EgfrLabResult"/>
/// </summary>
public class QuestLabResultHandler(
    ILogger<QuestLabResultHandler> logger,
    IMessageSession messageSession,
    IApplicationTime applicationTime)
    : IHandleEvent<EgfrLabResult>
{
    /// <summary>
    /// Send NSB event to handle request
    /// </summary>
    /// <param name="event"></param>
    /// <param name="cancellationToken"></param>
    public async Task Handle(EgfrLabResult @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received LabResult with CenseoId={CenseoId}", @event.CenseoId);
        @event.ReceivedByEgfrDateTime = applicationTime.UtcNow();
        await messageSession.SendLocal(@event, cancellationToken: cancellationToken);
        logger.LogInformation("End Handle LabResult {CenseoId}", @event.CenseoId);
    }
}