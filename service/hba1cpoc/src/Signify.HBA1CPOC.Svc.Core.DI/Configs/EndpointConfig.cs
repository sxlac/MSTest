using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using NServiceBus;
using NServiceBus.Transport;
using Signify.HBA1CPOC.Svc.Core.Configs;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Signify.HBA1CPOC.Messages.Events;

namespace Signify.HBA1CPOC.Svc.Core.DI.Configs;

public static class EndpointConfig
{
    public static EndpointConfiguration Create(IConfiguration config)
    {
        //Important!  As of 3/16/2021, the Serilog.Extensions.Hosting package must be version 3.0.0
        //For NSB to log properly.  The latest version today is 4.1.2 and no package above 3.0.0 to 4.1.2 works with NSB.
        //When you run the debugger you should see a ton of debug level messages related to NServiceBus
        //If you don't, first check that your Development log level is set to Debug
        //Then, if you still don't, make sure that Serilog.Extensions.Hosting did not inadvertently get upgraded.
        //You can try upgrading to the latest version if there is a later version than 4.1.2 to see if the issue is resolved,
        //Otherwise just revert to 3.0.0

        var nsbConfig = new ServiceBusConfig();
        config.GetSection(ServiceBusConfig.Key).Bind(nsbConfig);
        var endpointName = nsbConfig.QueueName;
        var endpointConfiguration = new EndpointConfiguration(endpointName);
        endpointConfiguration.CustomDiagnosticsWriter((_, _) => Task.CompletedTask);
        endpointConfiguration.UseSerialization<NewtonsoftJsonSerializer>();
        endpointConfiguration.LimitMessageProcessingConcurrencyTo(nsbConfig.MessageProcessingConcurrencyLimit);

        // Transport
        if (nsbConfig.TransportType.Equals("rabbitmq", StringComparison.InvariantCultureIgnoreCase))
        {
            var transport = new RabbitMQTransport(RoutingTopology.Conventional(QueueType.Quorum),
                config.GetConnectionString(nsbConfig.TransportConnection));

            ConfigureTransport(transport, nsbConfig, endpointConfiguration);
        }
        else // transportType == "AzureServiceBus"
        {
            var transport = new AzureServiceBusTransport(config.GetConnectionString(nsbConfig.TransportConnection));

            // If this is not specified, NSB uses default "bundle-1" topic name
            // Since local development has to share with deployed dev, use a unique configured topic name
            // when running locally, ex. "yourservicedomain.{yourname}".  All deployed environments can have same 'real'
            // topic name since they are in different Azure Service Bus namespaces, the problem is only between deployed dev
            // and local dev which share the dev Azure Service Bus namespace.
            transport.Topology = TopicTopology.Single(nsbConfig.TopicName);

            ConfigureTransport(transport, nsbConfig, endpointConfiguration);
        }

        // Errors and recovery
        endpointConfiguration.SendFailedMessagesTo($"{endpointName}.error");
        var recoverability = endpointConfiguration.Recoverability();
        recoverability.Immediate(immediate =>
        {
            immediate.NumberOfRetries(nsbConfig.ImmediateRetryCount);
        });

        recoverability.Delayed(delayed =>
        {
            delayed
                .NumberOfRetries(nsbConfig.DelayedRetryCount)
                .TimeIncrease(TimeSpan.FromSeconds(nsbConfig.DelayedRetrySecondsIncrease));
        });

        // Persistence
        var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
        var dialect = persistence.SqlDialect<SqlDialect.PostgreSql>();
        dialect.JsonBParameterModifier(parameter =>
        {
            var npgsqlParameter = (NpgsqlParameter)parameter;
            npgsqlParameter.NpgsqlDbType = NpgsqlDbType.Jsonb;
        });

        persistence.SagaSettings().JsonSettings(new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead
        });
        persistence.TablePrefix("HBA1CPOCServiceSaga_");

        var connectionString = config.GetConnectionString("DB");
        persistence.ConnectionBuilder(() => new NpgsqlConnection(connectionString));
        persistence.SubscriptionSettings().CacheFor(TimeSpan.FromMinutes(nsbConfig.PersistenceCacheMinutes));

        endpointConfiguration.EnableInstallers();

        return endpointConfiguration;
    }
    private static void ConfigureTransport(TransportDefinition transport, ServiceBusConfig nsbConfig, EndpointConfiguration endpointConfiguration)
    {
        endpointConfiguration.UseTransport(transport)
            .RouteToEndpoint(Assembly.GetAssembly(typeof(EvalReceived)), nsbConfig.QueueName);

        //Outbox pattern prevents duplicate message processing
        if (!nsbConfig.UseOutbox)
            return;

        transport.TransportTransactionMode = TransportTransactionMode.ReceiveOnly; // Required to use outbox

        var outboxSettings = endpointConfiguration.EnableOutbox();
        outboxSettings.KeepDeduplicationDataFor(TimeSpan.FromDays(nsbConfig.OutboxDedupDays));
        outboxSettings.RunDeduplicationDataCleanupEvery(TimeSpan.FromMinutes(nsbConfig.OutboxDedupCleanupMinutes));
    }
}