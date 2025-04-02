using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Signify.AkkaStreams.Kafka;
using Signify.AkkaStreams.Kafka.DependencyInjection;
using Signify.AkkaStreams.Postgres;
using Signify.GenericHost.Diagnostics.HealthChecks.DependencyInjection;
using Signify.GenericHost.Diagnostics.HealthChecks.Http;
using Signify.Spirometry.Core.ApiClients.AppointmentApi;
using Signify.Spirometry.Core.ApiClients.CdiApi.Flags;
using Signify.Spirometry.Core.ApiClients.CdiApi.Holds;
using Signify.Spirometry.Core.ApiClients.EvaluationApi;
using Signify.Spirometry.Core.ApiClients.MemberApi;
using Signify.Spirometry.Core.ApiClients.OktaApi;
using Signify.Spirometry.Core.ApiClients.ProviderApi;
using Signify.Spirometry.Core.ApiClients.ProviderPayApi;
using Signify.Spirometry.Core.ApiClients.RcmApi;
using Signify.Spirometry.Core.Behaviors;
using Signify.Spirometry.Core.Configs;
using Signify.Spirometry.Core.Configs.Exam;
using Signify.Spirometry.Core.Configs.Loopback;
using Signify.Spirometry.Core.Constants;
using Signify.Spirometry.Core.Converters;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.DI.Configs;
using Signify.Spirometry.Core.EventHandlers.Akka;
using Signify.Spirometry.Core.Events.Status;
using Signify.Spirometry.Core.Exceptions;
using Signify.Spirometry.Core.Factories;
using Signify.Spirometry.Core.FeatureFlagging;
using Signify.Spirometry.Core.Filters;
using Signify.Spirometry.Core.Infrastructure;
using Signify.Spirometry.Core.Maps;
using Signify.Spirometry.Core.Services;
using Signify.Spirometry.Core.Services.Flags;
using Signify.Spirometry.Core.Validators;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using Signify.Spirometry.Core.Events.Akka.DLQ;

namespace Signify.Spirometry.Core.DI;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static void AddCoreConfigs(this IServiceCollection services, IConfiguration config)
    {
        AddConfigs(services, config);

        AddSpirometryServices(services);

        AddAutoMapper(services);

        services.AddDbContext<SpirometryDataContext>(opts =>
            opts.UseNpgsql(config.GetConnectionString(ConnectionStringNames.SpirometryDatabase)));

        AddRefitClients(services);

        services.AddSignifyAkkaStreamsKafka(
            streamingOptions => SetupKafkaStreaming(streamingOptions, config),
            consumerOptions => SetupKafkaConsumer(consumerOptions, config, services),
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

    private static void SetupKafkaConsumer(ConsumerOptions consumerOptions, IConfiguration configuration,IServiceCollection services)
    {
        var config = configuration.GetRequiredSection(KafkaConsumerConfig.Key).Get<KafkaConsumerConfig>();

        consumerOptions.KafkaBrokers = config.Brokers;
        consumerOptions.KafkaGroupId = config.GroupId;

        SetupKafkaCredentials(consumerOptions.ConfigurationOptions, config);

        consumerOptions.AddAssemblyFromType<EvaluationFinalizedHandler>();

        consumerOptions.ContinueOnFailure = config.ContinueOnFailure;
        consumerOptions.ContinueOnDeserializationErrors = config.ContinueOnDeserializationErrors;
        consumerOptions.ContinueOnDeserializationErrors = services.BuildServiceProvider().GetRequiredService<IFeatureFlags>().EnableDlq;

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

        var statusTopic = config.Topics["Status"];
        var resultsTopic = config.Topics["Results"];
        var evaluationDlqTopic = config.Topics["EvaluationDlq"];
        var pdfDeliveryDlqTopic = config.Topics["PdfDeliveryDlq"];
        var overreadDlqTopic = config.Topics["OverreadDlq"];
        var cdiHoldsDlqTopic = config.Topics["CdiHoldsDlq"];
        var cdiEventsTopic = config.Topics["CdiEvents"];
        var rcmBillTopic = config.Topics["RcmBill"];

        producerOptions.SetTopicResolver(
            @event => //resolves the topic at time of publish so each publish call doesn't need to specify the specific topic.
            {
                return @event switch
                {
                    Performed => statusTopic,
                    NotPerformed => statusTopic,
                    BillRequestSent => statusTopic,
                    BillRequestNotSent => statusTopic,
                    ResultsReceived => statusTopic,
                    ProviderPayRequestSent => statusTopic,
                    ProviderNonPayableEventReceived => statusTopic,
                    ProviderPayableEventReceived => statusTopic,
                    FlaggedForLoopback => statusTopic,
                    Events.Akka.ResultsReceived => resultsTopic,
                    EvaluationDlqMessage => evaluationDlqTopic,
                    PdfDeliveryDlqMessage => pdfDeliveryDlqTopic,
                    OverreadDlqMessage => overreadDlqTopic,
                    CdiHoldsDlqMessage => cdiHoldsDlqTopic,
                    CdiEventDlqMessage => cdiEventsTopic,
                    RcmBillDlqMessage => rcmBillTopic,

                    _ => throw new KafkaPublishException("Unable to resolve outbound Kafka topic for message: " + @event)
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
        if (config.SecurityProtocol == null || !config.SecurityProtocol.Equals("sasl_ssl", StringComparison.OrdinalIgnoreCase))
            return;

        configurationOptions["security.protocol"] = config.SecurityProtocol;
        configurationOptions["sasl.mechanism"] = "PLAIN";
        configurationOptions["sasl.username"] = config.Username;
        configurationOptions["sasl.password"] = config.Password;
    }

    #endregion Akka Kafka

    private static void AddConfigs(IServiceCollection services, IConfiguration config)
    {
        TImplementation AddConfig<TConfig, TImplementation>(string section, bool isOptional = false, Action<TImplementation> configAction = null)
            where TImplementation : class, TConfig, new()
        {
            var subsection = isOptional ? config.GetSection(section) : config.GetRequiredSection(section);
            if (isOptional && subsection.Value == null)
                return null;

            var tConfig = subsection.Get<TImplementation>(opts => opts.BindNonPublicProperties = true) ?? new TImplementation();

            configAction?.Invoke(tConfig);

            services.AddSingleton(typeof(TConfig), tConfig);

            return tConfig;
        }

        AddConfig<WebApiConfig, WebApiConfig>(WebApiConfig.Key);
        AddConfig<ServiceBusConfig, ServiceBusConfig>(ServiceBusConfig.Key);
        AddConfig<OktaConfig, OktaConfig>(OktaConfig.Key);
        AddConfig<LoopbackConfig, LoopbackConfig>(LoopbackConfig.Key);
        AddConfig<ExamResultsConfig, ExamResultsConfig>(ExamResultsConfig.Key);
        services.AddSingleton<IFev1Config>(sp => sp.GetRequiredService<ExamResultsConfig>().Fev1);
        services.AddSingleton<IFvcConfig>(sp => sp.GetRequiredService<ExamResultsConfig>().Fvc);
        AddConfig<LaunchDarklyConfig, LaunchDarklyConfig>(LaunchDarklyConfig.Key);
    }

    private static void AddAutoMapper(IServiceCollection services)
    {
        services.AddAutoMapper(typeof(MappingProfile).Assembly);
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

    private static void AddSpirometryServices(IServiceCollection services)
    {
        services.AddSingleton<IFeatureFlags, FeatureFlags>();
        services.AddSingleton<IApplicationTime, ApplicationTime>();
        services.AddSingleton<IProductFilter, ProductFilter>();
        services.AddSingleton<IGetLoopbackConfig, LoopbackConfigManager>();
        services.AddSingleton<IExamQualityService, ExamQualityService>();
        services.AddSingleton<IFlagTextFormatter, FlagTextFormatter>();
        services.AddSingleton<IOverallNormalityConverter, OverallNormalityConverter>();
        services.AddSingleton<ITrileanTypeConverterFactory, TrileanTypeConverterFactory>();
        services.AddSingleton<IOccurrenceFrequencyConverterFactory, OccurrenceFrequencyConverterFactory>();
        services.AddSingleton<IFvcValidator, FvcValidator>();
        services.AddSingleton<IFev1Validator, Fev1Validator>();
        services.AddSingleton<IFev1FvcRatioValidator, Fev1FvcRatioValidator>();
        services.AddSingleton<IFvcNormalityConverter, FvcNormalityConverter>();
        services.AddSingleton<IFev1NormalityConverter, Fev1NormalityConverter>();
        services.AddSingleton<IBuildAnswerLookup, AnswerLookupBuilderService>();

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
            .AddDbContextCheck<SpirometryDataContext>(customTestQuery: async (ctx, cancellationToken) =>
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

        services.AddRefitClient<ICdiFlagsApi>()
            .ConfigureHttpClient((provider, client) =>
            {
                var config = provider.GetRequiredService<WebApiConfig>();
                client.BaseAddress = config.CdiFlagsApiUrl;
            })
            .AddHttpMessageHandler(provider => provider.GetRequiredService<OktaClientCredentialsHttpClientHandler>());

        services.AddRefitClient<ICdiHoldsApi>()
            .ConfigureHttpClient((provider, client) =>
            {
                var config = provider.GetRequiredService<WebApiConfig>();
                client.BaseAddress = config.CdiHoldsApiUrl;
            })
            .AddHttpMessageHandler(provider => provider.GetRequiredService<OktaClientCredentialsHttpClientHandler>());

        services.AddRefitClient<IEvaluationApi>()
            .ConfigureHttpClient((provider, client) =>
            {
                var config = provider.GetRequiredService<WebApiConfig>();
                client.BaseAddress = config.EvaluationApiUrl;
            })
            .AddHttpMessageHandler(provider => provider.GetRequiredService<OktaClientCredentialsHttpClientHandler>());

        services.AddRefitClient<IMemberApi>()
            .ConfigureHttpClient((provider, client) =>
            {
                var config = provider.GetRequiredService<WebApiConfig>();
                client.BaseAddress = config.MemberApiUrl;
            })
            .AddHttpMessageHandler(provider => provider.GetRequiredService<OktaClientCredentialsHttpClientHandler>());

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
            .AddHttpMessageHandler(provider => provider.GetRequiredService<OktaClientCredentialsHttpClientHandler>());

        services.AddRefitClient<IRcmApi>()
            .ConfigureHttpClient((provider, client) =>
            {
                var config = provider.GetRequiredService<WebApiConfig>();
                client.BaseAddress = config.RcmApiUrl;
            })
            .AddHttpMessageHandler(provider => provider.GetRequiredService<OktaClientCredentialsHttpClientHandler>());

        services.AddRefitClient<IAppointmentApi>()
            .ConfigureHttpClient((provider, client) =>
            {
                var config = provider.GetRequiredService<WebApiConfig>();
                client.BaseAddress = config.AppointmentApiUrl;
            })
            .AddHttpMessageHandler(provider => provider.GetRequiredService<OktaClientCredentialsHttpClientHandler>());

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