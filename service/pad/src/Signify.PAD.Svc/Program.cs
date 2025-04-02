using Microsoft.Extensions.Hosting;
using NServiceBus;
using Serilog;
using Signify.PAD.Svc.Core.DI;
using System.Diagnostics.CodeAnalysis;
using Signify.Dps.Observability.Library.ServiceCollection;
using Signify.PAD.Svc.Core.DI.Configs;

namespace Signify.PAD.Svc
{
    [ExcludeFromCodeCoverage]
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
                    services.AddObservabilityServices();
                });
    }
}
