using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Signify.AkkaStreams.Kafka.DependencyInjection;
using Signify.AkkaStreams.Postgres;
using Signify.FOBT.Messages.Events.Status;
using Signify.FOBT.Svc.Core.ApiClient;
using Signify.FOBT.Svc.Core.Behaviors;
using Signify.FOBT.Svc.Core.BusinessRules;
using Signify.FOBT.Svc.Core.Configs;
using Signify.FOBT.Svc.Core.Data;
using Signify.FOBT.Svc.Core.DI.Configs;
using Signify.FOBT.Svc.Core.EventHandlers;
using Signify.FOBT.Svc.Core.Events.Status;
using Signify.FOBT.Svc.Core.Events;
using Signify.FOBT.Svc.Core.Exceptions;
using Signify.FOBT.Svc.Core.FeatureFlagging;
using Signify.FOBT.Svc.Core.Filters;
using Signify.FOBT.Svc.Core.Infrastructure.Observability;
using Signify.FOBT.Svc.Core.Infrastructure;
using Signify.FOBT.Svc.Core.Maps;
using Signify.FOBT.Svc.Core.Validators;
using Signify.GenericHost.Diagnostics.HealthChecks.DependencyInjection;
using Signify.GenericHost.Diagnostics.HealthChecks.Http;
using System.Net.Http;
using System;
using Signify.FOBT.Svc.Core.Events.Akka.DLQ;
using OrderHeldStatus = Signify.FOBT.Svc.Core.Events.Status.OrderHeld;

namespace Signify.FOBT.Svc.Core.DI;

public static class ServiceCollectionHelper
{
    public static void AddCoreConfigs(this IServiceCollection services, IConfiguration config)
    {
        AddConfigs(services, config);

        AddFobtServices(services);

        AddAutoMapper(services);

        services.AddDbContext<FOBTDataContext>(opts =>
            opts.UseNpgsql(config.GetConnectionString("DB")!));

        AddRefitClients(services);

        var streamingConfig = new StreamingConfig();
        config.Bind("AkkaKafkaStream", streamingConfig);
        services.AddSignifyAkkaStreamsKafka(
            streamingOptions =>
            {
                streamingOptions.LogLevel = streamingConfig.LogLevel;
            },
            consumerOptions =>
            {
                consumerOptions.MinimumBackoffSeconds = streamingConfig.MinimumBackoffSeconds;
                consumerOptions.MaximumBackoffSeconds = streamingConfig.MaximumBackoffSeconds;
                consumerOptions.MaximumBackoffRetries = streamingConfig.MaximumBackoffRetries;
                consumerOptions.CommitMaxBatchSize = streamingConfig.CommitMaxBatchSize;
                consumerOptions.KafkaGroupId = streamingConfig.KafkaGroupId;
                consumerOptions.KafkaBrokers = streamingConfig.KafkaBrokers;
                if (streamingConfig.SecurityProtocol?.ToLower() == "sasl_ssl")
                {
                    consumerOptions.ConfigurationOptions["security.protocol"] = streamingConfig.SecurityProtocol;
                    consumerOptions.ConfigurationOptions["sasl.mechanism"] = streamingConfig.Mechanism;
                    consumerOptions.ConfigurationOptions["sasl.username"] = streamingConfig.Username;
                    consumerOptions.ConfigurationOptions["sasl.password"] = streamingConfig.Password;
                }
                consumerOptions.SubscribeTo("evaluation", "labs_barcode", "labs_holds", "homeaccess_labresults", "pdfdelivery", "cdi_events", "rcm_bill");
                consumerOptions.AddAssemblyFromType<EvaluationFinalizedHandler>();
                consumerOptions.ContinueOnFailure = streamingConfig.ContinueOnFailure;
                consumerOptions.ContinueOnDeserializationErrors = streamingConfig.ContinueOnDeserializationErrors;
                consumerOptions.ContinueOnDeserializationErrors = services.BuildServiceProvider().GetRequiredService<IFeatureFlags>().EnableDlq;
            },
            producerOptions =>
            {
                producerOptions.KafkaBrokers = streamingConfig.KafkaBrokers;
                producerOptions.ProducerInstances = streamingConfig.ProducerInstances; producerOptions.SetTopicResolver(@event =>
                {
                    return @event switch
                    {
                        Results => Constants.OutboundTopics.FOBT_Result,
                        Performed => Constants.OutboundTopics.FOBT_Status,
                        NotPerformed => Constants.OutboundTopics.FOBT_Status,
                        BillRequestSent => Constants.OutboundTopics.FOBT_Status,
                        BillRequestNotSent => Constants.OutboundTopics.FOBT_Status,
                        ProviderPayRequestSent => Constants.OutboundTopics.FOBT_Status,
                        // This has to be placed before call to ProviderPayableEventReceived as ProviderNonPayableEventReceived is derived from ProviderPayableEventReceived
                        ProviderNonPayableEventReceived => Constants.OutboundTopics.FOBT_Status,
                        ProviderPayableEventReceived => Constants.OutboundTopics.FOBT_Status,
                        OrderHeldStatus => Constants.OutboundTopics.FOBT_Status,
                        EvaluationDlqMessage => Constants.OutboundTopics.Dlq_Evaluation,
                        LabsBarcodeDlqMessage => Constants.OutboundTopics.Dlq_LabsBarcode,
                        LabsHoldsDlqMessage => Constants.OutboundTopics.Dlq_LabsHolds,
                        HomeaccessResultsDlqMessage => Constants.OutboundTopics.Dlq_HomeaccessLabresults,
                        PdfDeliveryDlqMessage => Constants.OutboundTopics.Dlq_Pdfdelivery,
                        CdiEventDlqMessage => Constants.OutboundTopics.Dlq_CdiEvents,
                        RcmBillDlqMessage => Constants.OutboundTopics.Dlq_RcmBill,
                        _ => throw new KafkaPublishException("Unable to resolve outbound Kafka topic for message: " + @event)
                    };
                });
                producerOptions.UsePostgres(ss =>
                {
                    ss.ConnectionString = streamingConfig.PersistenceConnection;
                    ss.Schema = streamingConfig.PersistenceSchema;
                    ss.MaxRetries = streamingConfig.PersistenceMaxRetries;
                    ss.PollingInterval = streamingConfig.PollingInterval;
                });
                if (streamingConfig.SecurityProtocol?.ToLower() == "sasl_ssl")
                {
                    producerOptions.ConfigurationOptions["security.protocol"] = streamingConfig.SecurityProtocol;
                    producerOptions.ConfigurationOptions["sasl.mechanism"] = "PLAIN";
                    producerOptions.ConfigurationOptions["sasl.username"] = streamingConfig.Username;
                    producerOptions.ConfigurationOptions["sasl.password"] = streamingConfig.Password;
                }
            });

        AddMediatR(services);

        RegisterCustomHealthChecks(services, config);
    }

    private static void AddConfigs(IServiceCollection services, IConfiguration config)
    {
        void AddConfig<TConfig>(string section) where TConfig : class
        {
            var subsection = config.GetSection(section)
                .Get<TConfig>(opts => opts.BindNonPublicProperties = true);
            services.AddSingleton(subsection);
        }

        AddConfig<WebApiConfig>("ApiUrls");
        AddConfig<OktaConfig>("Okta");
        AddConfig<ServiceBusConfig>("ServiceBus");
        AddConfig<LaunchDarklyConfig>("LaunchDarkly");
    }

    private static void AddAutoMapper(IServiceCollection services)
    {
        services.AddAutoMapper(typeof(MappingProfile).Assembly);
    }

    private static void AddMediatR(IServiceCollection services)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(LoggingBehavior<,>).Assembly);

            config.AddOpenBehavior(typeof(LoggingBehavior<,>), ServiceLifetime.Singleton);
            config.AddOpenBehavior(typeof(MediatrUnitOfWork<,>), ServiceLifetime.Scoped);
        });
    }

    private static void AddFobtServices(IServiceCollection services)
    {
        services.AddSingleton<IProductFilter, ProductFilter>();
        services.AddSingleton<ILabResultsValidator, LabResultsValidator>();
        services.AddSingleton<IFeatureFlags, FeatureFlags>();
        services.AddSingleton<IObservabilityService, ObservabilityService>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IApplicationTime, ApplicationTime>();
        services.AddSingleton<BillAndPayRules>();
        services.AddSingleton<IPayableRules>(sp=>sp.GetRequiredService<BillAndPayRules>());
        services.AddSingleton<IBillableRules>(sp=>sp.GetRequiredService<BillAndPayRules>());

        services.AddScoped<ITransactionSupplier, TransactionSupplier>();
        services.AddScoped<IPublishObservability, PublishObservability>();
    }

    private static void RegisterCustomHealthChecks(IServiceCollection services, IConfiguration config)
    {
        const string livenessTag = "Liveness";
        const string kafkaHealthCheckName = "KafkaConsumerHealthCheck";

        services.AddHealthChecks()
            .AddKafkaConsumerHealthCheck(kafkaHealthCheckName, configureOptions =>
            {
                // Optional configuration; defaults are also provided
                config.GetSection(kafkaHealthCheckName).Bind(configureOptions);
            }, new[] { livenessTag })
            .AddHttpHealthProbeListener(
                probeListenerOptions =>
                {
                    var options = new HttpHealthProbeListenerOptions();
                    config.Bind("LivenessProbe", options);
                    probeListenerOptions.Uri = options.Uri;
                },
                healthCheckOptions =>
                {
                    // Specify which health checks should be checked by this listener,
                    // or leave this unset to include all health checks
                    healthCheckOptions.Predicate = registration => registration.Tags.Contains(livenessTag);
                })
            .AddDbContextCheck<FOBTDataContext>();
    }

    private static void AddRefitClients(IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddScoped<OktaClientCredentialsHttpClientHandler>();

        services.AddRefitClient<IRcmApi>(new RefitSettings(new NewtonsoftJsonContentSerializer()))
            .ConfigureHttpClient((sp, c) =>
            {
                var config = sp.GetRequiredService<WebApiConfig>();
                c.BaseAddress = config.RcmApiUrl;
            }).AddHttpMessageHandler(s => s.GetService<OktaClientCredentialsHttpClientHandler>());

        services.AddRefitClient<IEvaluationApi>(new RefitSettings(new NewtonsoftJsonContentSerializer()))
            .ConfigureHttpClient((sp, c) =>
            {
                var config = sp.GetRequiredService<WebApiConfig>();
                c.BaseAddress = config.EvaluationApiUrl;
            }).AddHttpMessageHandler(s => s.GetService<OktaClientCredentialsHttpClientHandler>());

        services.AddRefitClient<IOktaApi>(new RefitSettings(new NewtonsoftJsonContentSerializer()))
            .ConfigureHttpClient((sp, c) =>
            {
                var config = sp.GetRequiredService<OktaConfig>();
                c.BaseAddress = config.Domain;
            });

        services.AddRefitClient<IInventoryApi>(new RefitSettings(new NewtonsoftJsonContentSerializer()))
            .ConfigureHttpClient((sp, c) =>
            {
                var config = sp.GetRequiredService<WebApiConfig>();
                c.BaseAddress = config.InventoryApiUrl;
            }).AddHttpMessageHandler(s => s.GetService<OktaClientCredentialsHttpClientHandler>());

        services.AddRefitClient<IMemberInfoApi>(new RefitSettings(new NewtonsoftJsonContentSerializer()))
            .ConfigureHttpClient((sp, c) =>
            {
                var config = sp.GetRequiredService<WebApiConfig>();
                c.BaseAddress = config.MemberApiUrl;
            }).AddHttpMessageHandler(s => s.GetService<OktaClientCredentialsHttpClientHandler>());

        services.AddRefitClient<IProviderApi>(new RefitSettings(new NewtonsoftJsonContentSerializer()))
            .ConfigureHttpClient((sp, c) =>
            {
                var config = sp.GetRequiredService<WebApiConfig>();
                c.BaseAddress = config.ProviderApiUrl;
            }).AddHttpMessageHandler(s => s.GetService<OktaClientCredentialsHttpClientHandler>());

        services.AddRefitClient<IProviderPayApi>(new RefitSettings(new NewtonsoftJsonContentSerializer()))
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
                
        services.AddRefitClient<ILabsApi>(new RefitSettings { ContentSerializer = new NewtonsoftJsonContentSerializer() })
            .ConfigureHttpClient((sp, c) =>
            {
                var config = sp.GetRequiredService<WebApiConfig>();
                c.BaseAddress = config.LabsApiUrl;
            }).AddHttpMessageHandler(s => s.GetService<OktaClientCredentialsHttpClientHandler>());
    }
}