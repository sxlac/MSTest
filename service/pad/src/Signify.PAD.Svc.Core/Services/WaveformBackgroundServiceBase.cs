using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.PAD.Svc.Core.Configs.WaveformConfigs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.PAD.Svc.Core.Services
{
    public abstract class WaveformBackgroundServiceBase : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IWaveformBackgroundServiceConfig _config;

        protected ILogger Logger { get; }
        
        protected WaveformBackgroundServiceBase(ILogger logger, IServiceScopeFactory serviceScopeFactory, IWaveformBackgroundServiceConfig config)
        {
            _serviceScopeFactory = serviceScopeFactory;
            Logger = logger;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.LogInformation("Starting service");

            var isFirstRun = true;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Don't delay the first run. Also, putting this delay after the `await Execute` call below
                    // would cause there to be no delay if an exception is thrown in that method.
                    if (!isFirstRun) 
                        await Task.Delay(TimeSpan.FromSeconds(_config.PollingPeriodSeconds), stoppingToken);
                    else
                        isFirstRun = false;

                    using var scope = _serviceScopeFactory.CreateScope();

                    Logger.LogInformation("Executing");

                    await Execute(scope.ServiceProvider, stoppingToken);
                }
                catch (ObjectDisposedException)
                {
                    // nothing to do, we're shutting down
                }
                catch (OperationCanceledException)
                {
                    // nothing to do, we're shutting down
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Execution loop failed - {Message}", ex.Message);
                }
            }

            Logger.LogInformation("Stopping service");
        }

        /// <summary>
        /// Method called on the configured interval
        /// </summary>
        /// <param name="serviceProvider">Service provider for this iteration</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Transaction]
        protected abstract Task Execute(IServiceProvider serviceProvider, CancellationToken cancellationToken);
    }
}
