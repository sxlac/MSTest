using Microsoft.Extensions.Hosting;
using NServiceBus;
using Serilog;
using Serilog.Enrichers.Span;
using Signify.Spirometry.Core.DI;
using Signify.Spirometry.Core.DI.Configs;
using System.Diagnostics.CodeAnalysis;
using Signify.Dps.Observability.Library.ServiceCollection;

namespace Signify.Spirometry.Svc
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
                    .Enrich.WithSpan()
                    .Enrich.WithThreadId())
                .UseNServiceBus(context => EndpointConfig.Create(context.Configuration))
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddCoreConfigs(hostContext.Configuration);
                    services.AddObservabilityServices();
                });
    }
}