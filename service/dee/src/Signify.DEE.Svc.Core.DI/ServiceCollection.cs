using Iris.Public.Image;
using Iris.Public.Order;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Refit;
using Signify.AkkaStreams.Kafka.DependencyInjection;
using Signify.AkkaStreams.Postgres;
using Signify.DEE.Messages;
using Signify.DEE.Messages.Status;
using Signify.DEE.Svc.Core.ApiClient;
using Signify.DEE.Svc.Core.ApiClients.CdiApi.Holds;
using Signify.DEE.Svc.Core.Behaviors;
using Signify.DEE.Svc.Core.BusinessRules;
using Signify.DEE.Svc.Core.Configs;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.EventHandlers;
using Signify.DEE.Svc.Core.Exceptions;
using Signify.DEE.Svc.Core.FeatureFlagging;
using Signify.DEE.Svc.Core.Filters;
using Signify.DEE.Svc.Core.Infrastructure;
using Signify.DEE.Svc.Core.Maps;
using Signify.DEE.Svc.Core.Messages.Services;
using Signify.DEE.Svc.Core.Services;
using Signify.GenericHost.Diagnostics.HealthChecks.DependencyInjection;
using Signify.GenericHost.Diagnostics.HealthChecks.Http;
using System;
using System.Net.Http;
using Signify.DEE.Svc.Core.Events.Akka.DLQ;

namespace Signify.DEE.Svc.Core.DI;

public static class ServiceCollectionHelper
{
    public static void AddCoreConfigs(this IServiceCollection services, IConfiguration config)
    {
        AddConfigs(services, config);

        AddDeeServices(services);

        AddMediatr(services);

        services.AddMemoryCache();
        services.AddScoped<OktaClientCredentialsHttpClientHandler>();
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        services.AddScoped(o => new OrderSubmissionService(config.GetValue<string>("Iris:OrderSubmissionServiceBusConnectionString")));
        services.AddScoped(o => new ImageSubmissionService(config.GetValue<string>("Iris:ImageUploadConnectionString")));
        services.AddDbContext<DataContext>(options => options.UseNpgsql(config.GetConnectionString("DB")));

        RegisterCustomHealthChecks(services, config);

        AddRefitClients(services);

        AddAutoMapper(services);

        var streamingConfig = new StreamingConfig();
        config.Bind("AkkaKafkaStream", streamingConfig);

        services.AddSignifyAkkaStreamsKafka(
            streamingOptions => { streamingOptions.LogLevel = streamingConfig.LogLevel; },
            consumerOptions =>
            {
                consumerOptions.MinimumBackoffSeconds = streamingConfig.MinimumBackoffSeconds;
                consumerOptions.MaximumBackoffSeconds = streamingConfig.MaximumBackoffSeconds;
                consumerOptions.MaximumBackoffRetries = streamingConfig.MaximumBackoffRetries;
                consumerOptions.CommitMaxBatchSize = streamingConfig.CommitMaxBatchSize;
                consumerOptions.KafkaGroupId = streamingConfig.KafkaGroupId;
                consumerOptions.KafkaBrokers = streamingConfig.KafkaBrokers;
                consumerOptions.ConfigurationOptions["security.protocol"] = streamingConfig.SecurityProtocol;
                consumerOptions.ConfigurationOptions["sasl.mechanism"] = streamingConfig.Mechanism;
                consumerOptions.ConfigurationOptions["sasl.username"] = streamingConfig.Username;
                consumerOptions.ConfigurationOptions["sasl.password"] = streamingConfig.Password;
                consumerOptions.SubscribeTo("evaluation", "pdfdelivery", "cdi_events", "rcm_bill", "cdi_holds");
                consumerOptions.AddAssemblyFromType<EvaluationFinalizedEventHandler>();
                consumerOptions.ContinueOnFailure = streamingConfig.ContinueOnFailure;
                consumerOptions.ContinueOnDeserializationErrors = streamingConfig.ContinueOnDeserializationErrors;
                consumerOptions.ContinueOnDeserializationErrors = services.BuildServiceProvider().GetRequiredService<IFeatureFlags>().EnableDlq;
            },
            producerOptions =>
            {
                services.AddScoped(typeof(IPipelineBehavior<,>), typeof(MediatrUnitOfWork<,>));
                producerOptions.KafkaBrokers = streamingConfig.KafkaBrokers;
                producerOptions.ProducerInstances =
                    streamingConfig.ProducerInstances; //increases the number of producers. May increase producing speed, but order cannot be guaranteed.
                producerOptions.SetTopicResolver(
                    @event => //resolves the topic at time of publish so each publish call doesn't need to specify the specific topic.
                    {
                        return @event switch
                        {
                            Performed => Constants.OutboundTopics.Dee_Status,
                            NotPerformed => Constants.OutboundTopics.Dee_Status,
                            BillRequestSent => Constants.OutboundTopics.Dee_Status,
                            BillRequestNotSent => Constants.OutboundTopics.Dee_Status,
                            ResultsReceived => Constants.OutboundTopics.Dee_Status,
                            ProviderPayRequestSent => Constants.OutboundTopics.Dee_Status,
                            ProviderNonPayableEventReceived => Constants.OutboundTopics.Dee_Status,
                            ProviderPayableEventReceived => Constants.OutboundTopics.Dee_Status,
                            EvaluationDlqMessage => Constants.OutboundTopics.Dlq_Evaluation,
                            PdfDeliveryDlqMessage => Constants.OutboundTopics.Dlq_Pdfdelivery,
                            CdiEventDlqMessage => Constants.OutboundTopics.Dlq_CdiEvents,
                            RcmBillDlqMessage => Constants.OutboundTopics.Dlq_RcmBill,
                            CdiHoldsDlqMessage => Constants.OutboundTopics.Dlq_CdiHolds,
                            Result => Constants.OutboundTopics.Dee_Results,

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
    }

    private static void AddIrisServices(IServiceCollection services)
    {
        // Register a long-running background service for subscribing to order results
        services.AddHostedService<OrderResultsBackgroundService>();
        services.AddHostedService<OrderEventsBackgroundService>();
    }

    private static void AddDeeServices(IServiceCollection services)
    {
        services.AddSingleton((IApplicationTime)new ApplicationTime());
        services.AddSingleton<IFeatureFlags, FeatureFlags>();
        services.AddSingleton<IProductFilter, ProductFilter>();

        services.AddSingleton(sp =>
            (IOverallNormalityMapper)new OverallNormalityMapper(sp.GetRequiredService<ILogger<OverallNormalityMapper>>()));
        services.AddSingleton((IDetermineGradability)new DetermineGradability());
        services.AddSingleton(sp =>
            (IDetermineBillability)new DetermineBillability(sp.GetRequiredService<IDetermineGradability>()));
        services.AddSingleton<BillAndPayRules>();
        services.AddSingleton<IPayableRules>(sp => sp.GetRequiredService<BillAndPayRules>());
        services.AddSingleton<IBillableRules>(sp => sp.GetRequiredService<BillAndPayRules>());
        services.AddScoped<TransactionSupplier>()
            .AddScoped<ITransactionSupplier>(sp => sp.GetRequiredService<TransactionSupplier>());

        AddIrisServices(services);
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

    private static void AddAutoMapper(IServiceCollection services)
    {
        services.AddAutoMapper(typeof(MappingProfile).Assembly);
    }

    private static void AddConfigs(IServiceCollection services, IConfiguration config)
    {
        void AddConfig<TConfig>(string section) where TConfig : class
        {
            var subsection = config.GetSection(section).Get<TConfig>(opts => opts.BindNonPublicProperties = true);
            services.AddSingleton(subsection);
        }

        AddConfig<WebApiConfig>("ApiUrls");
        AddConfig<ServiceBusConfig>("ServiceBus");
        AddConfig<OktaConfig>("Okta");
        AddConfig<IrisDocumentInfoConfig>("IrisDocumentInfo");
        AddConfig<ConnectionStringConfig>("ConnectionStrings");
        AddConfig<LaunchDarklyConfig>("LaunchDarkly");
        AddConfig<IrisConfig>("Iris");
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
            .AddDbContextCheck<DbContext>(customTestQuery: async (ctx, cancellationToken) =>
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
        services.AddRefitClient<ICdiHoldsApi>()
            .ConfigureHttpClient((provider, client) =>
            {
                var config = provider.GetRequiredService<WebApiConfig>();
                client.BaseAddress = config.CdiHoldsApiUrl;
            })
            .AddHttpMessageHandler(provider => provider.GetRequiredService<OktaClientCredentialsHttpClientHandler>());

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

        services.AddRefitClient<IMemberApi>()
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

        services.AddRefitClient<IRCMApi>()
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