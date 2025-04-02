using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Signify.AkkaStreams.Kafka;
using Signify.AkkaStreams.Kafka.DependencyInjection;
using Signify.AkkaStreams.Postgres;
using Signify.eGFR.Core.ApiClients.EvaluationApi;
using Signify.eGFR.Core.ApiClients.MemberApi;
using Signify.eGFR.Core.ApiClients.OktaApi;
using Signify.eGFR.Core.ApiClients.ProviderApi;
using Signify.eGFR.Core.ApiClients.RcmApi;
using Signify.eGFR.Core.Behaviors;
using Signify.eGFR.Core.Builders;
using Signify.eGFR.Core.Configs;
using Signify.eGFR.Core.Constants;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.DI.Configs;
using Signify.eGFR.Core.EventHandlers.Akka;
using Signify.eGFR.Core.Exceptions;
using Signify.eGFR.Core.Filters;
using Signify.eGFR.Core.Infrastructure;
using Signify.eGFR.Core.Maps;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using Signify.GenericHost.Diagnostics.HealthChecks.DependencyInjection;
using Signify.GenericHost.Diagnostics.HealthChecks.Http;
using Signify.eGFR.Core.ApiClients;
using Signify.eGFR.Core.ApiClients.InternalLabResultApi;
using Signify.eGFR.Core.ApiClients.ProviderPayApi;
using Signify.eGFR.Core.BusinessRules;
using Signify.eGFR.Core.Configs.Kafka;
using Signify.eGFR.Core.Events.Akka;
using Signify.eGFR.Core.Events.Akka.DLQ;
using Signify.eGFR.Core.Events.Status;
using Signify.eGFR.Core.FeatureFlagging;
using Signify.eGFR.Core.Infrastructure.Vendor;

namespace Signify.eGFR.Core.DI;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static void AddCoreConfigs(this IServiceCollection services, IConfiguration config)
    {
        AddConfigs(services, config);

        AddEgfrServices(services);

        AddAutoMapper(services);

        services.AddDbContext<DataContext>(opts =>
            opts.UseNpgsql(config.GetConnectionString(ConnectionStringNames.eGFRDatabase)));

        AddRefitClients(services);

        services.AddSignifyAkkaStreamsKafka(
            streamingOptions => SetupKafkaStreaming(streamingOptions, config),
            consumerOptions => SetupKafkaConsumer(consumerOptions, config),
            producerOptions => SetupKafkaProducer(producerOptions, config));

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

    private static void SetupKafkaConsumer(ConsumerOptions consumerOptions, IConfiguration configuration)
    {
        var config = configuration.GetRequiredSection(KafkaConsumerConfig.Key).Get<KafkaConsumerConfig>();
        var dlqConfig = configuration.GetRequiredSection(KafkaDlqConfig.Key).Get<KafkaDlqConfig>();

        consumerOptions.KafkaBrokers = config.Brokers;
        consumerOptions.KafkaGroupId = config.GroupId;

        SetupKafkaCredentials(consumerOptions.ConfigurationOptions, config);

        consumerOptions.AddAssemblyFromType<EvaluationFinalizedHandler>();

        consumerOptions.ContinueOnFailure = config.ContinueOnFailure;
        consumerOptions.ContinueOnDeserializationErrors = dlqConfig.IsDlqEnabled;

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
                    ResultsReceived => topics["Results"],
                    ProviderPayRequestSent => topics["Status"],
                    ProviderNonPayableEventReceived => topics["Status"],
                    ProviderPayableEventReceived => topics["Status"],
                    OrderCreationEvent => topics["Order"],
                    EvaluationDlqMessage => topics["DpsEvaluationDlq"],
                    PdfDeliveryDlqMessage => topics["DpsPdfDeliveryDlq"],
                    CdiEventDlqMessage => topics["DpsCdiEventDlq"],
                    RcmBillDlqMessage => topics["DpsRcmBillDlq"],
                    DpsLabResultDlqMessage => topics["DpsLabResultDlq"],
                    _ => throw new KafkaPublishException(
                        "Unable to resolve outbound Kafka topic for message: " + @event)
                };
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
        void AddConfig<TConfig>(string section) where TConfig : class
        {
            var subsection = config.GetRequiredSection(section)
                .Get<TConfig>(opts => opts.BindNonPublicProperties = true);
            services.AddSingleton(subsection);
        }

        AddConfig<WebApiConfig>(WebApiConfig.Key);
        AddConfig<OktaConfig>(OktaConfig.Key);
        AddConfig<ServiceBusConfig>(ServiceBusConfig.Key);
        AddConfig<LaunchDarklyConfig>("LaunchDarkly");
        AddConfig<NormalityIndicator>("NormalityIndicator");
        AddConfig<KafkaDlqConfig>(KafkaDlqConfig.Key);
    }

    private static void AddAutoMapper(IServiceCollection services)
    {
        services.AddAutoMapper(typeof(MappingProfile).Assembly);
        services.AddAutoMapper(typeof(KedEgfrLabResultMapper).Assembly);
    }

    private static void AddMediatr(IServiceCollection services)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(LoggingBehavior<,>).Assembly);

            config.AddOpenBehavior(typeof(LoggingBehavior<,>), ServiceLifetime.Singleton);
            // MediatrUnitOfWork apparently doesn't exist in eGFR at this time
            // Leaving as-is for now, because it's not necessarily required if you're using the ITransactionSupplier
        });
    }

    private static void AddEgfrServices(IServiceCollection services)
    {
        services.AddSingleton<IFeatureFlags, FeatureFlags>();
        services.AddSingleton<IApplicationTime, ApplicationTime>();
        services.AddSingleton<IProductFilter, ProductFilter>();
        services.AddSingleton<IExamModelBuilder, ExamModelBuilder>();
        services.AddSingleton<BillAndPayRules>();
        services.AddSingleton<IPayableRules>(sp => sp.GetRequiredService<BillAndPayRules>());
        services.AddSingleton<IBillableRules>(sp => sp.GetRequiredService<BillAndPayRules>());
        services.AddSingleton<IVendorDetermination, VendorDetermination>();

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
        services.AddMemoryCache();
        services.AddScoped<OAuthClientCredentialsHttpClientHandler>();

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

        services.AddRefitClient<IRcmApi>()
            .ConfigureHttpClient((provider, client) =>
            {
                var config = provider.GetRequiredService<WebApiConfig>();
                client.BaseAddress = config.RcmApiUrl;
            })
            .AddHttpMessageHandler(provider => provider.GetService<OAuthClientCredentialsHttpClientHandler>());

        services.AddRefitClient<IProviderPayApi>(new RefitSettings(new NewtonsoftJsonContentSerializer()))
            .ConfigureHttpClient((sp, c) =>
            {
                var config = sp.GetRequiredService<WebApiConfig>();
                c.BaseAddress = config.ProviderPayApiUrl;
            })
            .AddHttpMessageHandler(s => s.GetService<OAuthClientCredentialsHttpClientHandler>())
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                // to disable redirecting for 303 responses
                AllowAutoRedirect = false
            });

        services.AddRefitClient<IInternalLabResultApi>()
            .ConfigureHttpClient((provider, client) =>
            {
                var config = provider.GetRequiredService<WebApiConfig>();
                client.BaseAddress = config.InternalLabResultApiUrl;
            })
            .AddHttpMessageHandler(provider => provider.GetService<OAuthClientCredentialsHttpClientHandler>());
    }
}