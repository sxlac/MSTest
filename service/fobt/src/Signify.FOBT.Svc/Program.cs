using Microsoft.Extensions.Hosting;
using NServiceBus;
using Serilog;
using Signify.FOBT.Svc.Core.DI.Configs;
using Signify.FOBT.Svc.Core.DI;

namespace Signify.FOBT.Svc;

public static class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(hostingContext.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithThreadId())
            .UseNServiceBus(context => EndpointConfig.Create(context.Configuration))
            .ConfigureServices((hostContext, services) =>
            {
                services.AddCoreConfigs(hostContext.Configuration);
            });
}