using Iris.Public.Result.Azure;
using Iris.Public.Types.Models.V2_3_1;
using IrisPoc.Settings;
using Microsoft.Extensions.Options;

namespace IrisPoc.Services;

/// <summary>
/// Long-running background service that subscribes to order results, if enabled
/// </summary>
public sealed class OrderResultsBackgroundService : BackgroundService
{
    private readonly ILogger _logger;
    private readonly string _serviceBusConnectionString;
    private readonly StartupSettings _settings;

    // ReSharper disable once NotAccessedField.Local
    private OrderResultsService? _service;

    public OrderResultsBackgroundService(ILogger<OrderResultsBackgroundService> logger,
        string serviceBusConnectionString,
        IOptions<StartupSettings> options)
    {
        _logger = logger;
        _serviceBusConnectionString = serviceBusConnectionString;
        _settings = options.Value;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.SubscribeToResults)
        {
            _logger.LogInformation("Not subscribing to results");

            return Task.CompletedTask;
        }

        _service = new OrderResultsService(_serviceBusConnectionString, ResultHandler, stoppingToken);

        _logger.LogInformation("Subscribed to results");

        // Lifetime of this long-running service is the application shutdown token
        return Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private void ResultHandler(OrderResult obj)
    {
        // This doesn't log much at all at the moment; just put here so you can put a breakpoint and inspect the results and see when results are received
        _logger.LogInformation("Result received: {@R}", obj);
    }
}
