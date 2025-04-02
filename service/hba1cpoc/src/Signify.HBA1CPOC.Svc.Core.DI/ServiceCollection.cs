using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Signify.AkkaStreams.Kafka.DependencyInjection;
using Signify.AkkaStreams.Postgres;
using Signify.GenericHost.Diagnostics.HealthChecks.DependencyInjection;
using Signify.GenericHost.Diagnostics.HealthChecks.Http;
using Signify.HBA1CPOC.Messages.Events;
using Signify.HBA1CPOC.Messages.Events.Akka;
using Signify.HBA1CPOC.Messages.Events.Status;
using Signify.HBA1CPOC.Svc.Core.ApiClient;
using Signify.HBA1CPOC.Svc.Core.Behaviors;
using Signify.HBA1CPOC.Svc.Core.BusinessRules;
using Signify.HBA1CPOC.Svc.Core.Configs;
using Signify.HBA1CPOC.Svc.Core.Data;
using Signify.HBA1CPOC.Svc.Core.DI.Configs;
using Signify.HBA1CPOC.Svc.Core.EventHandlers;
using Signify.HBA1CPOC.Svc.Core.Exceptions;
using Signify.HBA1CPOC.Svc.Core.FeatureFlagging;
using Signify.HBA1CPOC.Svc.Core.Infrastructure;
using Signify.HBA1CPOC.Svc.Core.Infrastructure.Observability;
using Signify.HBA1CPOC.Svc.Core.Parsers;
using System;
using System.Net.Http;
using Signify.HBA1CPOC.Svc.Core.Configs.Kafka;
using Signify.HBA1CPOC.Svc.Core.Events;

namespace Signify.HBA1CPOC.Svc.Core.DI;

public static class ServiceCollectionHelper
{
    public static void AddCoreConfigs(this IServiceCollection services, IConfiguration config)
    {
        AddConfigs(services, config);

        AddHbaServices(services);

        services.AddSingleton(AutoMapperConfig.AddAutoMapper(services));
        services.AddDbContext<Hba1CpocDataContext>(opts => opts.UseNpgsql(config.GetConnectionString("DB")));

        AddRefitClients(services);

        var akkaConfig = new AffKaSteamConfig();
        config.Bind("AkkaKafkaStream", akkaConfig);

        var dlqConfig = config.GetRequiredSection(KafkaDlqConfig.Key).Get<KafkaDlqConfig>();
        services.AddSignifyAkkaStreamsKafka(
            streamingOptions => { streamingOptions.LogLevel = akkaConfig.LogLevel; },
            consumerOptions =>
            {
                consumerOptions.MinimumBackoffSeconds = akkaConfig.MinimumBackoffSeconds;
                consumerOptions.MaximumBackoffSeconds = akkaConfig.MaximumBackoffSeconds;
                consumerOptions.MaximumBackoffRetries = akkaConfig.MaximumBackoffRetries;
                consumerOptions.CommitMaxBatchSize = akkaConfig.CommitMaxBatchSize;
                consumerOptions.KafkaGroupId = akkaConfig.KafkaGroupId;
                consumerOptions.KafkaBrokers = akkaConfig.KafkaBrokers;
                if (akkaConfig.SecurityProtocol?.ToLower() == "sasl_ssl")
                {
                    consumerOptions.ConfigurationOptions["security.protocol"] = akkaConfig.SecurityProtocol;
                    consumerOptions.ConfigurationOptions["sasl.mechanism"] = akkaConfig.Mechanism;
                    consumerOptions.ConfigurationOptions["sasl.username"] = akkaConfig.Username;
                    consumerOptions.ConfigurationOptions["sasl.password"] = akkaConfig.Password;
                }
                consumerOptions.SubscribeTo("evaluation", "inventory", "pdfdelivery", "cdi_events", "rcm_bill");
                consumerOptions.AddAssemblyFromType<EvaluationFinalizedHandler>();
                consumerOptions.ContinueOnFailure = akkaConfig.ContinueOnFailure;
                consumerOptions.ContinueOnDeserializationErrors = dlqConfig.IsDlqEnabled;
            },
            producerOptions =>
            {
                producerOptions.KafkaBrokers = akkaConfig.KafkaBrokers;
                producerOptions.ProducerInstances = akkaConfig.ProducerInstances;
                producerOptions.UsePostgres(ss =>
                {
                    ss.ConnectionString = akkaConfig.PersistenceConnection;
                    ss.Schema = akkaConfig.PersistenceSchema;
                    ss.MaxRetries = akkaConfig.PersistenceMaxRetries;
                    ss.PollingInterval = akkaConfig.PollingInterval;
                });
                if (akkaConfig.SecurityProtocol?.ToLower() == "sasl_ssl")
                {
                    producerOptions.ConfigurationOptions["security.protocol"] = akkaConfig.SecurityProtocol;
                    producerOptions.ConfigurationOptions["sasl.mechanism"] = akkaConfig.Mechanism;
                    producerOptions.ConfigurationOptions["sasl.username"] = akkaConfig.Username;
                    producerOptions.ConfigurationOptions["sasl.password"] = akkaConfig.Password;
                }

                producerOptions.SetTopicResolver(@event =>
                {
                    return @event switch
                    {
                        A1CPOCPerformed => Constants.ApplicationConstants.OutboundTopics.HbA1cPoc_Performed,
                        ResultsReceived => Constants.ApplicationConstants.OutboundTopics.HbA1cPoc_Result,
                        Performed => Constants.ApplicationConstants.OutboundTopics.HbA1cPoc_Status,
                        NotPerformed => Constants.ApplicationConstants.OutboundTopics.HbA1cPoc_Status,
                        BillRequestSent => Constants.ApplicationConstants.OutboundTopics.HbA1cPoc_Status,
                        BillRequestNotSent => Constants.ApplicationConstants.OutboundTopics.HbA1cPoc_Status,
                        ProviderPayRequestSent => Constants.ApplicationConstants.OutboundTopics.HbA1cPoc_Status,
                        // This has to be placed before call to ProviderPayableEventReceived as ProviderNonPayableEventReceived is derived from ProviderPayableEventReceived
                        ProviderNonPayableEventReceived => Constants.ApplicationConstants.OutboundTopics.HbA1cPoc_Status,
                        ProviderPayableEventReceived => Constants.ApplicationConstants.OutboundTopics.HbA1cPoc_Status,
                        EvaluationDlqMessage => Constants.ApplicationConstants.OutboundTopics.DpsEvaluationDlq,
                        PdfDeliveryDlqMessage => Constants.ApplicationConstants.OutboundTopics.DpsPdfDeliveryDlq,
                        CdiEventDlqMessage => Constants.ApplicationConstants.OutboundTopics.DpsCdiEventDlq,
                        RcmBillDlqMessage => Constants.ApplicationConstants.OutboundTopics.DpsRcmBillDlq,
                        _ => throw new KafkaPublishException("Unable to resolve outbound Kafka topic for message: " + @event)
                    };
                });
            });
        AddMediatr(services);
        RegisterCustomHealthChecks(services, config);
    }

    private static void AddConfigs(IServiceCollection services, IConfiguration config, Action<object> onConfigAdded = null)
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
        AddConfig<LaunchDarklyConfig>("LaunchDarkly");
        AddConfig<KafkaDlqConfig>("KafkaDlq");
    }

    private static void AddMediatr(IServiceCollection services)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(LoggingBehavior<,>).Assembly);

            config.AddOpenBehavior(typeof(LoggingBehavior<,>), ServiceLifetime.Singleton);
            config.AddOpenBehavior(typeof(MediatrUnitOfWork<,>), ServiceLifetime.Scoped);
        });
    }

    private static void AddHbaServices(IServiceCollection services)
    {
        services.AddSingleton<IFeatureFlags, FeatureFlags>();
        services.AddSingleton<IObservabilityService, ObservabilityService>();
        services.AddSingleton<IApplicationTime, ApplicationTime>();
        services.AddSingleton<IResultsParser, ResultsParser>();
        services.AddSingleton<BillAndPayRules>();
        services.AddSingleton<IPayableRules>(sp => sp.GetRequiredService<BillAndPayRules>());
        services.AddSingleton<IBillableRules>(sp => sp.GetRequiredService<BillAndPayRules>());

        services.AddScoped<ITransactionSupplier, TransactionSupplier>();
        services.AddScoped<IPublishObservability, PublishObservability>();
    }

    private static void RegisterCustomHealthChecks(IServiceCollection services, IConfiguration config)
    {
        const string kafkaHealthCheckName = "KafkaConsumerHealthCheck";
        const string livenessTag = "LivenessHealthCheck";

        var nsbConfig = new ServiceBusConfig();
        config.GetSection("ServiceBus").Bind(nsbConfig);

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
            .AddDbContextCheck<Hba1CpocDataContext>(customTestQuery: async (ctx, cancellationToken) =>
            {
                try
                {
                    await ctx.Database.ExecuteSqlRawAsync("SELECT 1;", cancellationToken);
                    return true;
                }
                catch
                {
                    return false;
                }
            });
    }

    private static void AddRefitClients(IServiceCollection services)
    {
        services.AddMemoryCache();

        services.AddScoped<OktaClientCredentialsHttpClientHandler>();

        services.AddRefitClient<IEvaluationApi>()
            .ConfigureHttpClient((sp, c) =>
            {
                var config = sp.GetRequiredService<WebApiConfig>();
                c.BaseAddress = config.EvaluationApiUrl;
            }).AddHttpMessageHandler(s => s.GetService<OktaClientCredentialsHttpClientHandler>());

        services.AddRefitClient<IProviderApi>()
            .ConfigureHttpClient((sp, c) =>
            {
                var config = sp.GetRequiredService<WebApiConfig>();
                c.BaseAddress = config.ProviderApiUrl;
            }).AddHttpMessageHandler(s => s.GetService<OktaClientCredentialsHttpClientHandler>());

        services.AddRefitClient<IMemberInfoApi>()
            .ConfigureHttpClient((sp, c) =>
            {
                var config = sp.GetRequiredService<WebApiConfig>();
                c.BaseAddress = config.MemberApiUrl;
            }).AddHttpMessageHandler(s => s.GetService<OktaClientCredentialsHttpClientHandler>());

        services.AddRefitClient<IOktaApi>()
            .ConfigureHttpClient((sp, c) =>
            {
                var config = sp.GetRequiredService<OktaConfig>();
                c.BaseAddress = config.Domain;
            });

        services.AddRefitClient<IInventoryApi>()
            .ConfigureHttpClient((sp, c) =>
            {
                var config = sp.GetRequiredService<WebApiConfig>();
                c.BaseAddress = config.InventoryApiUrl;
            }).AddHttpMessageHandler(s => s.GetService<OktaClientCredentialsHttpClientHandler>());

        services.AddRefitClient<IRcmApi>()
            .ConfigureHttpClient((sp, c) =>
            {
                var config = sp.GetRequiredService<WebApiConfig>();
                c.BaseAddress = config.RcmApiUrl;
            }).AddHttpMessageHandler(s => s.GetService<OktaClientCredentialsHttpClientHandler>());

        services.AddRefitClient<IProviderPayApi>()
            .ConfigureHttpClient((sp, c) =>
            {
                var config = sp.GetRequiredService<WebApiConfig>();
                c.BaseAddress = config.ProviderPayApiUrl;
            })
            .AddHttpMessageHandler(s => s.GetService<OktaClientCredentialsHttpClientHandler>())
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                // to disable redirecting for 303 responses
                AllowAutoRedirect = false
            });
    }
}