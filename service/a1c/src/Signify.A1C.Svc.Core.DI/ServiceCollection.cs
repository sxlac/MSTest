using System;
using System.Reflection;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Serilog;
using Refit;
using Signify.A1C.Messages.Events;
using Signify.A1C.Svc.Core.ApiClient;
using Signify.A1C.Svc.Core.Behaviors;
using Signify.A1C.Svc.Core.Commands;
using Signify.A1C.Svc.Core.Configs;
using Signify.A1C.Svc.Core.Data;
using Signify.A1C.Svc.Core.Infrastructure;
using Signify.A1C.Svc.Core.Maps;
using Signify.A1C.Svc.Core.Sagas;
using Signify.AkkaStreams.Kafka.DependencyInjection;
using Signify.AkkaStreams.Postgres;
using Signify.A1C.Svc.Core.EventHandlers;
using Signify.GenericHost.Diagnostics.HealthChecks.DependencyInjection;
using Signify.GenericHost.Diagnostics.HealthChecks.Http;

namespace Signify.A1C.Svc.Core.DI
{
    public static class ServiceCollectionHelper
    {
        public static void AddCoreConfigs(this IServiceCollection services, IConfiguration config)
        {
            services.AddMediatR(typeof(CreateOrUpdateA1CHandler).GetTypeInfo().Assembly);


            services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

            AddConfigs(services, config);
            services.AddMemoryCache();

            services.AddScoped<OktaClientCredentialsHttpClientHandler>();

            services.AddDbContext<A1CDataContext>(opts => opts.UseNpgsql(config.GetConnectionString("DB")));

            //AutoMapper 
            var mappingConfig = new MapperConfiguration(mc => { mc.AddProfile(new MappingProfile()); });

            IMapper mapper = mappingConfig.CreateMapper();
            services.AddSingleton(mapper);

            RegisterCustomHealthChecks(services, config);

            AddRefitClients(services, config);

            var affkaStreamConfig = new AffKaSteamConfig();
            config.Bind("AkkaKafkaStream", affkaStreamConfig);

            services.AddSignifyAkkaStreamsKafka(
                streamingOptions =>
                {
                    streamingOptions.LogLevel = affkaStreamConfig.LogLevel;
                },
                consumerOptions =>
                {
                    AddNServiceBus(services, config);
                    consumerOptions.MinimumBackoffSeconds = affkaStreamConfig.MinimumBackoffSeconds;
                    consumerOptions.MaximumBackoffSeconds = affkaStreamConfig.MaximumBackoffSeconds;
                    consumerOptions.MaximumBackoffRetries = affkaStreamConfig.MaximumBackoffRetries;
                    consumerOptions.CommitMaxBatchSize = affkaStreamConfig.CommitMaxBatchSize;
                    consumerOptions.KafkaGroupId = affkaStreamConfig.KafkaGroupId;
                    consumerOptions.KafkaBrokers = affkaStreamConfig.KafkaBrokers;
                    consumerOptions.ConfigurationOptions["security.protocol"] = affkaStreamConfig.SecurityProtocol;
                    consumerOptions.ConfigurationOptions["sasl.mechanism"] = affkaStreamConfig.Mechanism;
                    consumerOptions.ConfigurationOptions["sasl.username"] = affkaStreamConfig.Username;
                    consumerOptions.ConfigurationOptions["sasl.password"] = affkaStreamConfig.Password;
                    consumerOptions.SubscribeTo("evaluation", "inventory", "labs_barcode", "homeaccess_labresults");
                    consumerOptions.AddAssemblyFromType<EvaluationFinalizedHandler>();
                    consumerOptions.ContinueOnFailure = affkaStreamConfig.ContinueOnFailure;
                    consumerOptions.ContinueOnDeserializationErrors = affkaStreamConfig.ContinueOnDeserializationErrors;
                },
                setupProducer: producerOptions =>
                {
                    services.AddScoped(typeof(IPipelineBehavior<,>), typeof(MediatrUnitOfWork<,>));
                    producerOptions.KafkaBrokers = affkaStreamConfig.KafkaBrokers;
                    producerOptions.ProducerInstances = affkaStreamConfig.ProducerInstances;
                    producerOptions.UsePostgres(ss =>
                    {
                        ss.ConnectionString = affkaStreamConfig.PersistenceConnection;
                        ss.Schema = affkaStreamConfig.PersistenceSchema;
                        ss.MaxRetries = affkaStreamConfig.PersistenceMaxRetries;
                        ss.PollingInterval = affkaStreamConfig.PollingInterval;
                    });
                    if (affkaStreamConfig.SecurityProtocol?.ToLower() == "sasl_ssl")
                    {
                        producerOptions.ConfigurationOptions["security.protocol"] = affkaStreamConfig.SecurityProtocol;
                        producerOptions.ConfigurationOptions["sasl.mechanism"] = affkaStreamConfig.Mechanism;
                        producerOptions.ConfigurationOptions["sasl.username"] = affkaStreamConfig.Username;
                        producerOptions.ConfigurationOptions["sasl.password"] = affkaStreamConfig.Password;
                    }
                });
        }

        private static void AddConfigs(IServiceCollection services, IConfiguration config,
            Action<object> onConfigAdded = null)
        {
            void AddConfig<TConfig>(string section) where TConfig : class
            {
                var subsection = config.GetSection(section).Get<TConfig>(opts => opts.BindNonPublicProperties = true);
                services.AddSingleton(subsection);

                onConfigAdded?.Invoke(subsection);
            }

            AddConfig<WebApiConfig>("ApiUrls");
            AddConfig<OktaConfig>("Okta");
            AddConfig<ServiceBusConfig>("ServiceBus");
        }

        private static void RegisterCustomHealthChecks(IServiceCollection services, IConfiguration config)
        {
            const string kafkaHealthCheckName = "KafkaConsumerHealthCheck";
            const string livenessTag = "LivenessHealthCheck";

            services.AddHealthChecks()
                .AddKafkaConsumerHealthCheck(kafkaHealthCheckName, configureOptions =>
                {
                    // Optional configuration; defaults are also provided
                    config.GetSection(kafkaHealthCheckName)?.Bind(configureOptions);
                }, new[] { livenessTag })
                .AddHttpHealthProbeListener(
                    probeListenerOptions =>
                    {
                        var configuration = new HttpHealthProbeListenerOptions();
                        config.Bind("LivenessProbe", configuration);
    
                        probeListenerOptions.Uri = configuration.Uri;
                    },
                    healthCheckOptions =>
                    {
                        // Specify which health checks should be checked by this listener,
                        // or leave this unset to include all health checks
                        healthCheckOptions.Predicate = registration => registration.Tags.Contains(livenessTag);
                    })
                .AddDbContextCheck<A1CDataContext>(customTestQuery: async (ctx, cancellationToken) =>
                {
                    try
                    {
                        await ctx.Database.ExecuteSqlRawAsync("SELECT 1;", cancellationToken);
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                });
        }

        private static void AddRefitClients(IServiceCollection services, IConfiguration configuration)
        {
            services.AddRefitClient<IEvaluationApi>(new RefitSettings() { ContentSerializer = new NewtonsoftJsonContentSerializer() })
                .ConfigureHttpClient((sp, c) =>
                {
                    var config = sp.GetRequiredService<WebApiConfig>();
                    c.BaseAddress = config.EvaluationApiUrl;
                }).AddHttpMessageHandler(s => s.GetService<OktaClientCredentialsHttpClientHandler>());

            services.AddRefitClient<IOktaApi>(new RefitSettings() { ContentSerializer = new NewtonsoftJsonContentSerializer() })
                .ConfigureHttpClient((sp, c) =>
                {
                    var config = sp.GetRequiredService<OktaConfig>();
                    c.BaseAddress = config.Domain;
                });


            services.AddRefitClient<IInventoryApi>(new RefitSettings() { ContentSerializer = new NewtonsoftJsonContentSerializer() })
                .ConfigureHttpClient((sp, c) =>
                {
                    var config = sp.GetRequiredService<WebApiConfig>();
                    c.BaseAddress = config.InventoryApiUrl;
                }).AddHttpMessageHandler(s => s.GetService<OktaClientCredentialsHttpClientHandler>());

            services.AddRefitClient<IMemberInfoApi>(new RefitSettings() { ContentSerializer = new NewtonsoftJsonContentSerializer() })
                .ConfigureHttpClient((sp, c) =>
                {
                    var config = sp.GetRequiredService<WebApiConfig>();
                    c.BaseAddress = config.MemberApiUrl;
                }).AddHttpMessageHandler(s => s.GetService<OktaClientCredentialsHttpClientHandler>());

            services.AddRefitClient<IProviderApi>(new RefitSettings() { ContentSerializer = new NewtonsoftJsonContentSerializer() })
                .ConfigureHttpClient((sp, c) =>
                {
                    var config = sp.GetRequiredService<WebApiConfig>();
                    c.BaseAddress = config.ProviderApiUrl;
                }).AddHttpMessageHandler(s => s.GetService<OktaClientCredentialsHttpClientHandler>());

            services.AddRefitClient<ILabsApi>(new RefitSettings() { ContentSerializer = new NewtonsoftJsonContentSerializer() })
                .ConfigureHttpClient((sp, c) =>
                {
                    var config = sp.GetRequiredService<WebApiConfig>();
                    c.BaseAddress = config.LabsApiUrl;
                }).AddHttpMessageHandler(s => s.GetService<OktaClientCredentialsHttpClientHandler>());
        }

        private static void AddNServiceBus(IServiceCollection services, IConfiguration config)
        {
            var nsbConfig = new ServiceBusConfig();
            config.GetSection("ServiceBus").Bind(nsbConfig);
            var endpointName = nsbConfig.QueueName;

            var connectionString = config.GetConnectionString("DB");
            var transportConnectionString = config.GetConnectionString("AzureServiceBus");

            var endpointConfiguration = new EndpointConfiguration(endpointName);
            endpointConfiguration.UseSerialization<NewtonsoftSerializer>();

            // Transport
            var transport = endpointConfiguration.UseTransport<AzureServiceBusTransport>();
            transport.ConnectionString(transportConnectionString);

            // If this is not specified, NSB uses default "bundle-1" topic name
            // Since local development has to share with deployed dev, use a unique configured topic name
            // when running locally, ex. "yourservicedomain.{yourname}".  All deployed environments can have same 'real'
            // topic name since they are in different Azure Service Bus namespaces, the problem is only between deployed dev
            // and local dev which share the dev Azure Service Bus namespace.
            transport.TopicName(nsbConfig.TopicName);

            //Outbox pattern prevents duplicate message processing.
            if (nsbConfig.UseOutbox)
            {
                var outboxSettings = endpointConfiguration.EnableOutbox();
                outboxSettings.KeepDeduplicationDataFor(TimeSpan.FromDays(nsbConfig.OutboxDedupDays));
                outboxSettings.RunDeduplicationDataCleanupEvery(
                    TimeSpan.FromMinutes(nsbConfig.OutboxDedupCleanupMinutes));
            }

            transport.Routing()
                .RouteToEndpoint(Assembly.GetAssembly(typeof(UpdateInventorySaga)), endpointName);

            transport.Routing()
                .RouteToEndpoint(Assembly.GetAssembly(typeof(A1CEvaluationReceived)), endpointName);

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
            persistence.TablePrefix("HBA1CServiceSaga_");

            persistence.ConnectionBuilder(connectionBuilder: () => new NpgsqlConnection(connectionString));
            persistence.SubscriptionSettings().CacheFor(TimeSpan.FromMinutes(nsbConfig.PersistenceCacheMinutes));

            endpointConfiguration.EnableInstallers();

            var containerSettings = endpointConfiguration.UseContainer(new DefaultServiceProviderFactory());
            containerSettings.ServiceCollection.Add(services);
            LogManager.Use<SerilogFactory>();

            var endpoint = Endpoint.Start(endpointConfiguration).GetAwaiter().GetResult();

            services.AddSingleton(endpoint);
        }
    }
}