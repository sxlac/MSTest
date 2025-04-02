using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using NServiceBus.Transport;
using NServiceBus;
using Signify.FOBT.Messages.Events;
using Signify.FOBT.Svc.Core.Configs;
using Signify.FOBT.Svc.Core.Sagas;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using System;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Signify.FOBT.Svc.Core.DI.Configs;

[ExcludeFromCodeCoverage]
public static class EndpointConfig
{
    public static EndpointConfiguration Create(IConfiguration config)
    {
        var nsbConfig = new ServiceBusConfig();
        config.GetSection("ServiceBus").Bind(nsbConfig);

        var endpointName = nsbConfig.QueueName;

        var endpointConfiguration = new EndpointConfiguration(endpointName);
        endpointConfiguration.CustomDiagnosticsWriter((_, _) => Task.CompletedTask);
        endpointConfiguration.UseSerialization<NewtonsoftJsonSerializer>();

        if (nsbConfig.TransportType.Equals("rabbitmq", StringComparison.InvariantCultureIgnoreCase))
        {
            var transport1 = new RabbitMQTransport(RoutingTopology.Conventional(QueueType.Quorum),
                config.GetConnectionString(nsbConfig.TransportConnection));
            ConfigureTransport(transport1, nsbConfig, endpointConfiguration);
        }
        else // transportType == "AzureServiceBus"
        {
            var transport = new AzureServiceBusTransport(config.GetConnectionString(nsbConfig.TransportConnection))
            {
                // If this is not specified, NSB uses default "bundle-1" topic name
                // Since local development has to share with deployed dev, use a unique configured topic name
                // when running locally, ex. "yourservicedomain.{yourname}".  All deployed environments can have same 'real'
                // topic name since they are in different Azure Service Bus namespaces, the problem is only between deployed dev
                // and local dev which share the dev Azure Service Bus namespace.
                Topology = TopicTopology.Single(nsbConfig.TopicName)
            };

            ConfigureTransport(transport, nsbConfig, endpointConfiguration);
        }

        // Errors and recovery
        endpointConfiguration.SendFailedMessagesTo($"{endpointName}.error");
        var recoverability = endpointConfiguration.Recoverability();
        recoverability.Immediate(
            immediate => { immediate.NumberOfRetries(nsbConfig.ImmediateRetryCount); });

        recoverability.Delayed(
            delayed =>
            {
                delayed.NumberOfRetries(nsbConfig.DelayedRetryCount)
                    .TimeIncrease(TimeSpan.FromSeconds(nsbConfig.DelayedRetrySecondsIncrease));
            });

        // Persistence
        var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
        var dialect = persistence.SqlDialect<SqlDialect.PostgreSql>();
        dialect.JsonBParameterModifier(
            parameter =>
            {
                var npgsqlParameter = (NpgsqlParameter)parameter;
                npgsqlParameter.NpgsqlDbType = NpgsqlDbType.Jsonb;
            });
        persistence.SagaSettings().JsonSettings(new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead
        });
        persistence.TablePrefix("FOBTServiceSaga_");

        var connectionString = config.GetConnectionString("DB");
        persistence.ConnectionBuilder(connectionBuilder: () => new NpgsqlConnection(connectionString));
        persistence.SubscriptionSettings().CacheFor(TimeSpan.FromMinutes(nsbConfig.PersistenceCacheMinutes));

        endpointConfiguration.EnableInstallers();

        return endpointConfiguration;
    }

    private static void ConfigureTransport(TransportDefinition transport, ServiceBusConfig nsbConfig, EndpointConfiguration endpointConfiguration)
    {
        endpointConfiguration.UseTransport(transport)
            .RouteToEndpoint(Assembly.GetAssembly(typeof(EvaluationReceived)), nsbConfig.QueueName);
        endpointConfiguration.UseTransport(transport)
            .RouteToEndpoint(Assembly.GetAssembly(typeof(UpdateInventorySaga)), nsbConfig.QueueName);

        //Outbox pattern prevents duplicate message processing.
        if (!nsbConfig.UseOutbox) 
            return;
        
        transport.TransportTransactionMode = TransportTransactionMode.ReceiveOnly; // Required to use outbox

        var outboxSettings = endpointConfiguration.EnableOutbox();
        outboxSettings.KeepDeduplicationDataFor(TimeSpan.FromDays(nsbConfig.OutboxDedupDays));
        outboxSettings.RunDeduplicationDataCleanupEvery(TimeSpan.FromMinutes(nsbConfig.OutboxDedupCleanupMinutes));
    }
}