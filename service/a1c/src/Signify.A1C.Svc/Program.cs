using Microsoft.Extensions.Hosting;
using Serilog;
using Signify.A1C.Svc.Core.DI;

namespace Signify.A1C.Svc
{
    public class Program
    {
        protected Program()
        {

        }

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
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddCoreConfigs(hostContext.Configuration);
                });
    }
}