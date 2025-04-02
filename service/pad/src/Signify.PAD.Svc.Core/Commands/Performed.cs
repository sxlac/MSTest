using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.PAD.Svc.Core.Models;
using System.Threading;
using System.Threading.Tasks;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Svc.Core.Constants;

namespace Signify.PAD.Svc.Core.Commands;

[ExcludeFromCodeCoverage]
public class Performed : PadStatusCode, IRequest<bool>;

public class PadPerformedHandler : IRequestHandler<Performed, bool>
{
    private readonly ILogger<PadPerformedHandler> _log;
    private readonly IMessageProducer _messageProducer;
    private readonly IObservabilityService _observabilityService;
        
    public PadPerformedHandler(
        ILogger<PadPerformedHandler> log, 
        IMessageProducer messageProducer,
        IObservabilityService observabilityService)
    {
        _log = log;
        _messageProducer = messageProducer;
        _observabilityService = observabilityService;
    }

    public async Task<bool> Handle(Performed request, CancellationToken cancellationToken)
    {
        _log.LogInformation($"Received PublishPadPerformed request  for: {request.ProductCode}, EvaluationId: {request.EvaluationId}");

        await _messageProducer.Produce(request.EvaluationId.ToString(), request, cancellationToken).ConfigureAwait(false);

        _log.LogInformation($"Published PadPerformed request  for: {request.ProductCode}, EvaluationId: {request.EvaluationId} on PAD_Status Topic");

        //add observability for dps evaluation dashboard
        _observabilityService.AddEvent(Observability.Evaluation.EvaluationPerformedEvent, new Dictionary<string, object>()
        {
            {Observability.EventParams.EvaluationId, request.EvaluationId},
            {Observability.EventParams.CreatedDateTime, request.CreateDate.ToUnixTimeSeconds()}
        });
        return true;
    }
}