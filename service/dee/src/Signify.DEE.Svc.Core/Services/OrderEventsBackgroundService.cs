using Iris.Public.Order;
using Iris.Public.Types.Models.Public._2._3._1;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Signify.DEE.Svc.Core.Configs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Services;

/// <summary>
/// Long-running background service that subscribes to order events, if enabled
/// </summary>
public sealed class OrderEventsBackgroundService(
    ILogger<OrderEventsBackgroundService> logger,
    IMessageSession messageSession,
    IrisConfig irisConfig)
    : BackgroundService
{
    // ReSharper disable once NotAccessedField.Local
#pragma warning disable S4487 // Unread "private" fields should be removed
    private OrderEventService _service;
#pragma warning restore S4487 // Unread "private" fields should be removed

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _service = new OrderEventService(irisConfig.OrderEventsServiceBusConnectionString,
            orderReceiptAction: OrderReceiptHandler,
            stoppingToken,
            imageReceiptAction: ImageReceiptHandler,
            errorHandler: ErrorHandler);

        logger.LogInformation("Subscribed to Iris events service bus");

        // Lifetime of this long-running service is the application shutdown token
        return Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task OrderReceiptHandler(OrderReceipt obj, CancellationToken cancellationToken)
    {
        await messageSession.SendLocal(obj, cancellationToken).ConfigureAwait(false);
        // This doesn't log much at all at the moment; just put here so you can put a breakpoint and inspect the results and see when orders are received
        logger.LogInformation("Order received: {@R}", obj);
    }

    private async Task ImageReceiptHandler(ImageReceipt obj, CancellationToken cancellationToken)
    {
        await messageSession.SendLocal(obj, cancellationToken).ConfigureAwait(false);
        // This doesn't log much at all at the moment; just put here so you can put a breakpoint and inspect the results and see when images are received
        logger.LogInformation("Image received: {@R}", obj);
    }

    private Task ErrorHandler(Exception ex, CancellationToken cancellationToken)
    {
        logger.LogWarning(ex, "Error while streaming results: {ErrorMessage}", ex.Message);
        return Task.CompletedTask;
    }

}