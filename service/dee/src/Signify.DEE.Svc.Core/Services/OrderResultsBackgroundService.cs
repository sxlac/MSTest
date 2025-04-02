using Iris.Public.Result.Azure;
using Iris.Public.Types.Models.V2_3_1;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Signify.DEE.Svc.Core.Configs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Services;

/// <summary>
/// Long-running background service that subscribes to order results, if enabled
/// </summary>
public sealed class OrderResultsBackgroundService(
    ILogger<OrderResultsBackgroundService> logger,
    IMessageSession messageSession,
    ConnectionStringConfig connectionStringConfig)
    : BackgroundService
{
    // ReSharper disable once NotAccessedField.Local
    private OrderResultsService _service;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _service = new OrderResultsService(connectionStringConfig.IrisResultDeliveryServiceBus,
            ResultHandler,
            ErrorHandler,
            stoppingToken);

        logger.LogInformation("Subscribed to Iris results service bus");

        // Lifetime of this long-running service is the application shutdown token
        return Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ResultHandler(OrderResult obj, CancellationToken cancellationToken)
    {
        await messageSession.SendLocal(obj, cancellationToken).ConfigureAwait(false);
        // This doesn't log much at all at the moment; just put here so you can put a breakpoint and inspect the results and see when results are received
        logger.LogInformation("Result received: {@R}", obj);
    }

    private Task ErrorHandler(Exception ex, CancellationToken cancellationToken)
    {
        logger.LogWarning(ex, "Error while streaming results: {ErrorMessage}", ex.Message);
        return Task.CompletedTask;
    }
}