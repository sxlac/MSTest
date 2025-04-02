using System;
using Microsoft.Extensions.Hosting;
using Serilog;
using Signify.CKD.Svc.Core.DI;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using NServiceBus;
using Signify.CKD.Messages.Events;
using Signify.CKD.Svc.Core.Configs;
using Signify.CKD.Svc.Core.DI.Configs;
using Signify.CKD.Svc.Core.Sagas;

namespace Signify.CKD.Svc
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
                });
    }
}