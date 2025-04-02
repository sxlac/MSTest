using System;
using System.Net.Http;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Signify.AkkaStreams.Kafka.DependencyInjection;
using Signify.AkkaStreams.Postgres;
using Signify.CKD.Svc.Core.ApiClient;
using Signify.CKD.Svc.Core.Behaviors;
using Signify.CKD.Svc.Core.BusinessRules;
using Signify.CKD.Svc.Core.Configs;
using Signify.CKD.Svc.Core.Constants;
using Signify.CKD.Svc.Core.Data;
using Signify.CKD.Svc.Core.DI.Configs;
using Signify.CKD.Svc.Core.EventHandlers;
using Signify.CKD.Svc.Core.Exceptions;
using Signify.CKD.Svc.Core.FeatureFlagging;
using Signify.CKD.Svc.Core.Filters;
using Signify.CKD.Svc.Core.Infrastructure;
using Signify.CKD.Svc.Core.Infrastructure.Observability;
using Signify.CKD.Svc.Core.Maps;
using Signify.CKD.Svc.Core.Messages.Status;
using Signify.CKD.Svc.Core.Models;
using Signify.GenericHost.Diagnostics.HealthChecks.DependencyInjection;
using Signify.GenericHost.Diagnostics.HealthChecks.Http;
using Result = Signify.CKD.Svc.Core.Messages.Result;

namespace Signify.CKD.Svc.Core.DI;

public static class ServiceCollectionHelper
{
    public static void AddCoreConfigs(this IServiceCollection services, IConfiguration config)
    {
        AddConfigs(services, config);

        AddCkdServices(services);

        AddAutoMapper(services);

        services.AddDbContext<CKDDataContext>(opts => opts.UseNpgsql(config.GetConnectionString("DB")));

        AddRefitClients(services);

        var akkaConfig = new AffKaSteamConfig();
        config.Bind("AkkaKafkaStream", akkaConfig);
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

                consumerOptions.SubscribeTo("evaluation", "inventory", "pdfdelivery", "cdi_events");
                consumerOptions.AddAssemblyFromType<EvaluationFinalizedHandler>();
                consumerOptions.ContinueOnFailure = akkaConfig.ContinueOnFailure;
                consumerOptions.ContinueOnDeserializationErrors = akkaConfig.ContinueOnDeserializationErrors;
            },
            producerOptions =>
            {
                producerOptions.KafkaBrokers = akkaConfig.KafkaBrokers;
                producerOptions.ProducerInstances = akkaConfig.ProducerInstances;
                producerOptions.SetTopicResolver(@event =>
                {
                    return @event switch
                    {
                        Performed => KafkaTopics.CkdStatus,
                        NotPerformed => KafkaTopics.CkdStatus,
                        BillRequestSent => KafkaTopics.CkdStatus,
                        BillRequestNotSent => KafkaTopics.CkdStatus,
                        ProviderPayRequestSent => KafkaTopics.CkdStatus,
                        // This has to be placed before call to ProviderPayableEventReceived as ProviderNonPayableEventReceived is derived from ProviderPayableEventReceived
                        ProviderNonPayableEventReceived => KafkaTopics.CkdStatus,
                        ProviderPayableEventReceived => KafkaTopics.CkdStatus,
                        Result => KafkaTopics.CkdResult,
                        _ => throw new KafkaPublishException("Unable to resolve outbound Kafka topic for message: " + @event)
                    };
                });
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
    }

    private static void AddAutoMapper(IServiceCollection services)
    {
        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddProfile<MappingProfile>();
            mc.ConstructServicesUsing(type =>
                ActivatorUtilities.CreateInstance(services.BuildServiceProvider(), type));
        });

        var mapper = mappingConfig.CreateMapper();
        services.AddSingleton(mapper);
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
            .AddDbContextCheck<CKDDataContext>(customTestQuery: async (ctx, cancellationToken) =>
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

    private static void AddCkdServices(IServiceCollection services)
    {
        services.AddSingleton(NewRelic.Api.Agent.NewRelic.GetAgent());
        services.AddSingleton<IObservabilityService, ObservabilityService>();
        services.AddSingleton<IApplicationTime, ApplicationTime>();
        services.AddSingleton<IFeatureFlags, FeatureFlags>();
        services.AddSingleton<IProductFilter, ProductFilter>();
        services.AddSingleton<BillAndPayRules>();
        services.AddSingleton<IPayableRules>(sp => sp.GetRequiredService<BillAndPayRules>());
        services.AddSingleton<IBillableRules>(sp => sp.GetRequiredService<BillAndPayRules>());

        services.AddScoped<ITransactionSupplier, TransactionSupplier>();
        services.AddScoped<IPublishObservability, PublishObservability>();
    }
}