using IrisPoc.Services.Orders;
using IrisPoc.Settings;
using Microsoft.Extensions.Options;

namespace IrisPoc.Services;

/// <summary>
/// Background service that can take image and order details from config and upload them to IRIS, if enabled
/// </summary>
public class OrderSubmissionBackgroundService : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IOrderSubmissionService _orderService;
    private readonly StartupSettings _settings;

    public OrderSubmissionBackgroundService(ILogger<OrderSubmissionBackgroundService> logger,
        IOrderSubmissionService orderService,
        IOptions<StartupSettings> options)
    {
        _logger = logger;
        _orderService = orderService;
        _settings = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.SubmitOrders)
        {
            _logger.LogInformation("Not placing orders");
            return;
        }

        foreach (var orderModel in _settings.Orders)
        {
            await _orderService.SubmitRequest(orderModel, stoppingToken);
        }

        _logger.LogInformation("Submitted {Count} orders", _settings.Orders.Count);
    }
}
