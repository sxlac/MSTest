using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using NServiceBus;
using Signify.AkkaStreams.Kafka;
using Signify.eGFR.Core.Constants;
using Signify.eGFR.Core.Events.Akka;
using Signify.eGFR.Core.FeatureFlagging;
using Signify.eGFR.Core.Filters;
using Signify.eGFR.Core.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Signify.eGFR.Core.EventHandlers.Akka;

/// <summary>
/// Base class for handling cdi_events.
/// Checks if the PM's product code is present, and raises a NSB command.
/// </summary>
public abstract class CdiEventHandlerBase<TE, TH>(
    ILogger<TH> logger,
    IMessageSession messageSession,
    IFeatureFlags featureFlags,
    IProductFilter productFilter,
    IApplicationTime applicationTime)
    where TE : CdiEventBase
    where TH : IHandleEvent<TE>
{
    [Transaction]
    protected async Task ValidateAndRaiseNsbCommand(TE cdiEvent, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Received {Event} with {Count} product codes: {ProductCodes}, for EvaluationId={EvaluationId} with RequestId={RequestId}",
            typeof(TE), cdiEvent.Products?.Count, GetProductCodes(cdiEvent.Products), cdiEvent.EvaluationId,
            cdiEvent.RequestId);

        if (!productFilter.ShouldProcess(cdiEvent.Products))
        {
            logger.LogInformation(
                "Evaluation ignored as product code is not present, EvaluationID:{EvaluationId}, RequestId: {RequestId}",
                cdiEvent.EvaluationId, cdiEvent.RequestId);
            return;
        }

        logger.LogInformation(
            "Evaluation Identified with product code {ProductCode}, EvaluationID : {EvaluationId}, RequestId: {RequestId}",
            ProductCodes.eGFR, cdiEvent.EvaluationId, cdiEvent.RequestId);

        if (!featureFlags.EnableProviderPayCdi)
        {
            logger.LogInformation(
                "CDI based ProviderPay feature is NOT enabled; hence NOT handling {EventType} for {EvaluationId}, RequestId: {RequestId}",
                typeof(TE), cdiEvent.EvaluationId, cdiEvent.RequestId);
            return;
        }

        cdiEvent.ReceivedByEgfrDateTime = applicationTime.UtcNow();
        
        await messageSession.SendLocal(cdiEvent, cancellationToken);

        logger.LogInformation(
            "Event queued for processing, for EvaluationId={EvaluationId} with EventId={RequestId}. End of {Handle}",
            @cdiEvent.EvaluationId, cdiEvent.RequestId, typeof(TH));
    }

    /// <summary>
    /// Gets a comma separated list of product codes
    /// </summary>
    /// <param name="products"></param>
    /// <returns></returns>
    private static string GetProductCodes(IReadOnlyCollection<DpsProduct> products) => products == null ? string.Empty : string.Join(',', products.Select(p => p.ProductCode));
}