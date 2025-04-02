using MediatR;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.PAD.Svc.Core.Models;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace Signify.PAD.Svc.Core.Commands;

[ExcludeFromCodeCoverage]
public class BillRequestSent : PadStatusCode, IRequest<bool>
{
    public string BillingProductCode { get; set; }
    public string BillId { get; set; }
    public DateTime PdfDeliveryDate { get; set; }
}

public class BillRequestSentHandler : IRequestHandler<BillRequestSent, bool>
{
    private readonly ILogger<BillRequestSentHandler> _log;
    private readonly IMessageProducer _messageProducer;

    public BillRequestSentHandler(ILogger<BillRequestSentHandler> log, IMessageProducer messageProducer)
    {
        _log = log;
        _messageProducer = messageProducer;
    }

    public async Task<bool> Handle(BillRequestSent request, CancellationToken cancellationToken)
    {
        _log.LogInformation($"Received PublishBillRequestSent request  for: {request.ProductCode}, EvaluationId: {request.EvaluationId}");
        await _messageProducer.Produce(request.EvaluationId.ToString(), request, cancellationToken).ConfigureAwait(false);
        _log.LogInformation($"Published BillRequestSent request  for: {request.ProductCode}, EvaluationId: {request.EvaluationId} on PAD_Status Topic");
            
        return true;
    }
}