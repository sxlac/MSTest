using Microsoft.Extensions.Hosting;
using Serilog;
using Signify.DEE.Svc.Core.DI;
using Serilog.Enrichers.Span;
using NServiceBus;
using Signify.DEE.Svc.Core.Configs;
using Microsoft.Extensions.Configuration;
using Signify.Dps.Observability.Library.ServiceCollection;

namespace Signify.DEE.Svc;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseNServiceBus(context =>
            {
                var config = context.Configuration;
                var nsbConfig = new ServiceBusConfig();
                config.GetSection("ServiceBus").Bind(nsbConfig);
                var endpointName = nsbConfig.QueueName;

                var endpointConfiguration = new EndpointConfiguration(endpointName);
                endpointConfiguration.UseSerialization<NewtonsoftJsonSerializer>();
                endpointConfiguration.LimitMessageProcessingConcurrencyTo(nsbConfig.MessageProcessingConcurrencyLimit);

                // Transport
                var transportConnectionString = config.GetConnectionString("AzureServiceBus");
                var transport = new AzureServiceBusTransport(transportConnectionString);
                endpointConfiguration.UseTransport(transport);
                    
                // Errors and recovery
                endpointConfiguration.SendFailedMessagesTo($"{endpointName}.error");
                endpointConfiguration.DisableFeature<NServiceBus.Features.Sagas>();
                endpointConfiguration.EnableInstallers();

                return endpointConfiguration;
            })
            .UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(hostingContext.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithSpan()
                .Enrich.WithThreadId())
            .ConfigureServices((hostContext, services) =>
            {
                services.AddCoreConfigs(hostContext.Configuration);
                services.AddObservabilityServices();
            });
}