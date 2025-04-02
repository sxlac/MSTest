using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Signify.AkkaStreams.Kafka;
using Signify.AkkaStreams.Kafka.DependencyInjection;
using Signify.AkkaStreams.Postgres;
using Signify.GenericHost.Diagnostics.HealthChecks.DependencyInjection;
using Signify.GenericHost.Diagnostics.HealthChecks.Http;
using Signify.uACR.Core.ApiClients.EvaluationApi;
using Signify.uACR.Core.ApiClients.MemberApi;
using Signify.uACR.Core.ApiClients.OktaApi;
using Signify.uACR.Core.ApiClients.ProviderApi;
using Signify.uACR.Core.ApiClients.RcmApi;
using Signify.uACR.Core.Behaviors;
using Signify.uACR.Core.Builders;
using Signify.uACR.Core.Configs;
using Signify.uACR.Core.Data;
using Signify.uACR.Core.DI.Configs;
using Signify.uACR.Core.EventHandlers.Akka;
using Signify.uACR.Core.Exceptions;
using Signify.uACR.Core.Filters;
using Signify.uACR.Core.Infrastructure;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using Signify.uACR.Core.ApiClients;
using Signify.uACR.Core.ApiClients.InternalLabResultApi;
using Signify.uACR.Core.ApiClients.ProviderPayAPi;
using Signify.uACR.Core.BusinessRules;
using Signify.uACR.Core.Constants;
using Signify.uACR.Core.Events.Akka;
using Signify.uACR.Core.Events.Akka.DLQ;
using Signify.uACR.Core.Events.Status;
using Signify.uACR.Core.FeatureFlagging;
using Signify.uACR.Core.Infrastructure.Vendor;
using KafkaTopics = Signify.uACR.Core.Configs.KafkaTopics;

namespace Signify.uACR.Core.DI;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static void AddCoreConfigs(this IServiceCollection services, IConfiguration config)
    {
        AddConfigs(services, config);

        AddUacrServices(services);

        services.AddSingleton(AutoMapperConfig.AddAutoMapper(services));
        services.AddDbContext<DataContext>(opts =>
            opts.UseNpgsql(config.GetConnectionString(ConnectionStringNames.UACRDatabase)));

        AddRefitClients(services);

        services.AddSignifyAkkaStreamsKafka(
            streamingOptions => SetupKafkaStreaming(streamingOptions, config),
            consumerOptions => SetupKafkaConsumer(consumerOptions, config, services.BuildServiceProvider()),
            producerOptions => SetupKafkaProducer(producerOptions, config)
        );

        AddMediatr(services);

        RegisterCustomHealthChecks(services, config);
    }

    #region Akka Kafka

    private static void SetupKafkaStreaming(StreamingOptions streamingOptions, IConfiguration configuration)
    {
        var section = configuration.GetSection(KafkaStreamingConfig.Key);

        var config = section.Exists() ? section.Get<KafkaStreamingConfig>() : new KafkaStreamingConfig();

        if (!string.IsNullOrEmpty(config.LogLevel))
            streamingOptions.LogLevel = config.LogLevel;
    }

    private static void SetupKafkaConsumer(ConsumerOptions consumerOptions, IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        var config = configuration.GetRequiredSection(KafkaConsumerConfig.Key).Get<KafkaConsumerConfig>();

        consumerOptions.KafkaBrokers = config.Brokers;
        consumerOptions.KafkaGroupId = config.GroupId;

        SetupKafkaCredentials(consumerOptions.ConfigurationOptions, config);

        consumerOptions.AddAssemblyFromType<EvaluationFinalizedHandler>();

        consumerOptions.ContinueOnFailure = config.ContinueOnFailure;
        consumerOptions.ContinueOnDeserializationErrors = config.ContinueOnDeserializationErrors;
        consumerOptions.ContinueOnDeserializationErrors = serviceProvider.GetRequiredService<IFeatureFlags>().EnableDlq;

        // Only override defaults set in Signify.AkkaStreams.Kafka.ConsumerOptions if they're set in our config
        if (config.MinimumBackoffSeconds > 0)
            consumerOptions.MinimumBackoffSeconds = config.MinimumBackoffSeconds;
        if (config.MaximumBackoffSeconds > 0)
            consumerOptions.MaximumBackoffSeconds = config.MaximumBackoffSeconds;
        if (config.MaximumBackoffRetries > 0)
            consumerOptions.MaximumBackoffRetries = config.MaximumBackoffRetries;
        if (config.MaximumBackoffRetriesWithinSeconds > 0)
            consumerOptions.MaximumBackoffRetriesWithinSeconds = config.MaximumBackoffRetriesWithinSeconds;

        if (config.CommitMaxBatchSize > 0)
            consumerOptions.CommitMaxBatchSize = config.CommitMaxBatchSize;

        if (!config.Topics.Any())
        {
            throw new ConfigurationErrorsException(
                $"Configuration section {KafkaConsumerConfig.Key} does not contain any Kafka topics to subscribe to");
        }

        consumerOptions.SubscribeTo(config.Topics.Values.ToArray());
    }

    private static void SetupKafkaProducer(ProducerOptions producerOptions, IConfiguration configuration)
    {
        var config = configuration.GetRequiredSection(KafkaProducerConfig.Key).Get<KafkaProducerConfig>();

        producerOptions.KafkaBrokers = config.Brokers;

        SetupKafkaCredentials(producerOptions.ConfigurationOptions, config);

        producerOptions.ProducerInstances = config.ProducerInstances;

        producerOptions.UsePostgres(postgresOptions =>
        {
            postgresOptions.ConnectionString = config.PersistenceConnection;
            postgresOptions.Schema = config.PersistenceSchema;
            postgresOptions.MaxRetries = config.PersistenceMaxRetries;
            postgresOptions.PollingInterval = config.PollingInterval;
        });

        if (!config.Topics.Any())
        {
            throw new ConfigurationErrorsException(
                $"Configuration section {KafkaProducerConfig.Key} does not contain any Kafka topics to publish to");
        }

        var topics = config.Topics;
        producerOptions.SetTopicResolver(
            @event => //resolves the topic at time of publish so each publish call doesn't need to specify the specific topic.
            {
                return @event switch
                {
                    Performed => topics["Status"],
                    NotPerformed => topics["Status"],
                    BillRequestSent => topics["Status"],
                    BillRequestNotSent => topics["Status"],
                    ProviderPayRequestSent => topics["Status"],
                    ProviderNonPayableEventReceived => topics["Status"],
                    ProviderPayableEventReceived => topics["Status"],
                    ResultsReceived => topics["Results"],
                    OrderCreationEvent => topics["Order"],
                    EvaluationDlqMessage => topics["DpsEvaluationDlq"],
                    PdfDeliveryDlqMessage => topics["DpsPdfDeliveryDlq"],
                    CdiEventDlqMessage => topics["DpsCdiEventDlq"],
                    RcmBillDlqMessage => topics["DpsRcmBillDlq"],
                    DpsLabResultDlqMessage => topics["DpsLabResultDlq"],
                    DpsRmsLabResultDlqMessage => topics["DpsRmsLabResultDlq"],
                    _ => throw new KafkaPublishException(
                        "Unable to resolve outbound Kafka topic for message: " + @event)
                };
            });

        producerOptions.UsePostgres(ss =>
        {
            ss.ConnectionString = config.PersistenceConnection;
            ss.Schema = config.PersistenceSchema;
            ss.MaxRetries = config.PersistenceMaxRetries;
            ss.PollingInterval = config.PollingInterval;
        });
    }

    private static void SetupKafkaCredentials(IDictionary<string, string> configurationOptions,
        BaseKafkaConfig config)
    {
        if (config.SecurityProtocol == null ||
            !config.SecurityProtocol.Equals("sasl_ssl", StringComparison.OrdinalIgnoreCase))
            return;

        configurationOptions["security.protocol"] = config.SecurityProtocol;
        configurationOptions["sasl.mechanism"] = "PLAIN";
        configurationOptions["sasl.username"] = config.Username;
        configurationOptions["sasl.password"] = config.Password;
    }

    #endregion Akka Kafka

    private static void AddConfigs(IServiceCollection services, IConfiguration config)
    {
        AddConfig<WebApiConfig>(WebApiConfig.Key);
        AddConfig<OktaConfig>(OktaConfig.Key);
        AddConfig<ServiceBusConfig>(ServiceBusConfig.Key);
        AddConfig<LaunchDarklyConfig>("LaunchDarkly");
        AddConfig<NormalityIndicator>("NormalityIndicator");
        AddConfig<KafkaTopics>(KafkaTopics.Key);
        return;

        void AddConfig<TConfig>(string section) where TConfig : class
        {
            var subsection = config.GetRequiredSection(section)
                .Get<TConfig>(opts => opts.BindNonPublicProperties = true);
            services.AddSingleton(subsection);
        }
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

    private static void AddUacrServices(IServiceCollection services)
    {
        services.AddSingleton<IFeatureFlags, FeatureFlags>();
        services.AddSingleton<IApplicationTime, ApplicationTime>();
        services.AddSingleton<IProductFilter, ProductFilter>();
        services.AddSingleton<IExamModelBuilder, ExamModelBuilder>();
        services.AddSingleton<IVendorDetermination, VendorDetermination>();
        services.AddSingleton<BillAndPayRules>();
        services.AddSingleton<IPayableRules>(sp => sp.GetRequiredService<BillAndPayRules>());
        services.AddSingleton<IBillableRules>(sp => sp.GetRequiredService<BillAndPayRules>());
        services.AddScoped<ITransactionSupplier, TransactionSupplier>();
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
            }, [livenessTag])
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
            .AddDbContextCheck<DataContext>(customTestQuery: async (ctx, cancellationToken) =>
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
        services.AddScoped<OAuthClientCredentialsHttpClientHandler>();
        services.AddMemoryCache();

        services.AddRefitClient<IEvaluationApi>()
            .ConfigureHttpClient((provider, client) =>
            {
                var config = provider.GetRequiredService<WebApiConfig>();
                client.BaseAddress = config.EvaluationApiUrl;
            })
            .AddHttpMessageHandler(provider => provider.GetService<OAuthClientCredentialsHttpClientHandler>());

        services.AddRefitClient<IMemberApi>()
            .ConfigureHttpClient((provider, client) =>
            {
                var config = provider.GetRequiredService<WebApiConfig>();
                client.BaseAddress = config.MemberApiUrl;
            })
            .AddHttpMessageHandler(provider => provider.GetService<OAuthClientCredentialsHttpClientHandler>());

        services.AddRefitClient<IOktaApi>()
            .ConfigureHttpClient((provider, client) =>
            {
                var config = provider.GetRequiredService<OktaConfig>();
                client.BaseAddress = config.Domain;
            });

        services.AddRefitClient<IProviderApi>()
            .ConfigureHttpClient((provider, client) =>
            {
                var config = provider.GetRequiredService<WebApiConfig>();
                client.BaseAddress = config.ProviderApiUrl;
            })
            .AddHttpMessageHandler(provider => provider.GetService<OAuthClientCredentialsHttpClientHandler>());

        services.AddRefitClient<IProviderPayApi>()
            .ConfigureHttpClient((provider, client) =>
            {
                var config = provider.GetRequiredService<WebApiConfig>();
                client.BaseAddress = config.ProviderPayApiUrl;
            })
            .AddHttpMessageHandler(provider => provider.GetService<OAuthClientCredentialsHttpClientHandler>())
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                // to disable redirecting for 303 responses
                AllowAutoRedirect = false
            });

        services.AddRefitClient<IRcmApi>()
            .ConfigureHttpClient((provider, client) =>
            {
                var config = provider.GetRequiredService<WebApiConfig>();
                client.BaseAddress = config.RcmApiUrl;
            })
            .AddHttpMessageHandler(provider => provider.GetService<OAuthClientCredentialsHttpClientHandler>());

        services.AddRefitClient<IInternalLabResultApi>()
            .ConfigureHttpClient((provider, client) =>
            {
                var config = provider.GetRequiredService<WebApiConfig>();
                client.BaseAddress = config.InternalLabResultApiUrl;
            })
            .AddHttpMessageHandler(provider => provider.GetService<OAuthClientCredentialsHttpClientHandler>());
    }
}