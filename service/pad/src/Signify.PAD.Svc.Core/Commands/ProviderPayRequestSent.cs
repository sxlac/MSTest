using MediatR;
using Microsoft.Extensions.Logging;
using Signify.AkkaStreams.Kafka;
using Signify.PAD.Svc.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Svc.Core.Constants;

namespace Signify.PAD.Svc.Core.Commands;

[ExcludeFromCodeCoverage]
public class ProviderPayRequestSent : PadStatusCode, IRequest<bool>
{
    public string ProviderPayProductCode { get; set; }
    public string PaymentId { get; set; }
    public DateTime PdfDeliveryDate { get; set; }
}

public class ProviderPaySentHandler : IRequestHandler<ProviderPayRequestSent, bool>
{
    private readonly ILogger<ProviderPaySentHandler> _logger;
    private readonly IMessageProducer _messageProducer;
    private readonly IObservabilityService _observabilityService;

    public ProviderPaySentHandler(
        ILogger<ProviderPaySentHandler> logger, 
        IMessageProducer messageProducer,
        IObservabilityService observabilityService)
    {
        _logger = logger;
        _messageProducer = messageProducer;
        _observabilityService = observabilityService;
    }

    public async Task<bool> Handle(ProviderPayRequestSent request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received {Request} request for EvaluationId: {EvaluationId}",
            nameof(ProviderPayRequestSent), request.EvaluationId);
        await _messageProducer.Produce(request.EvaluationId.ToString(), request, cancellationToken)
            .ConfigureAwait(false);
        _logger.LogInformation(
            "Published {Request} request for EvaluationId: {EvaluationId}",
            nameof(ProviderPayRequestSent), request.EvaluationId);
            
        //add observability for dps evaluation dashboard
        _observabilityService.AddEvent(Observability.ProviderPay.PayableCdiEvents, new Dictionary<string, object>()
        {
            {Observability.EventParams.CreatedDateTime, request.CreateDate.ToUnixTimeSeconds()},
            {Observability.EventParams.EvaluationId, request.EvaluationId},
            {Observability.EventParams.PaymentId, request.PaymentId}
        });
            
        return true;
    }
}