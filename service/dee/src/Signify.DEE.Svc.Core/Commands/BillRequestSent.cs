using MediatR;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Signify.DEE.Svc.Core.Messages.Models;

namespace Signify.DEE.Svc.Core.Commands;

[ExcludeFromCodeCoverage]
public class BillRequestSent : DeeStatusCode, IRequest<bool>
{
    public static string BillingProductCode => Constants.ApplicationConstants.ProductCode;
    public string BillId { get; set; }
    public DateTimeOffset? PdfDeliveryDate { get; set; }
}

public class BillRequestSentHandler(ILogger<BillRequestSentHandler> log, IMessageProducer messageProducer)
    : IRequestHandler<BillRequestSent, bool>
{
    public async Task<bool> Handle(BillRequestSent request, CancellationToken cancellationToken)
    {
        log.LogInformation("Received Publish BillRequestSent request for: {ProductCode}, EvaluationId: {EvaluationId}", request.ProductCode, request.EvaluationId);
        await messageProducer.Produce(request.EvaluationId.ToString(), request, cancellationToken).ConfigureAwait(false);
        log.LogInformation("Published BillRequestSent request for: {ProductCode}, EvaluationId: {EvaluationId} on Status Topic", request.ProductCode, request.EvaluationId);
            
        return true;
    }
}