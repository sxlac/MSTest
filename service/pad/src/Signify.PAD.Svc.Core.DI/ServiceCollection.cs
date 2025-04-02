using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Signify.AkkaStreams.Kafka.DependencyInjection;
using Signify.AkkaStreams.Postgres;
using Signify.GenericHost.Diagnostics.HealthChecks.DependencyInjection;
using Signify.GenericHost.Diagnostics.HealthChecks.Http;
using Signify.PAD.Svc.Core.ApiClient;
using Signify.PAD.Svc.Core.Behaviors;
using Signify.PAD.Svc.Core.BusinessRules;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Configs;
using Signify.PAD.Svc.Core.Configs.WaveformConfigs;
using Signify.PAD.Svc.Core.Converter;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.DI.Configs;
using Signify.PAD.Svc.Core.EventHandlers;
using Signify.PAD.Svc.Core.Events;
using Signify.PAD.Svc.Core.Events.Akka.DLQ;
using Signify.PAD.Svc.Core.Events.Status;
using Signify.PAD.Svc.Core.Exceptions;
using Signify.PAD.Svc.Core.FeatureFlagging;
using Signify.PAD.Svc.Core.Filters;
using Signify.PAD.Svc.Core.Infrastructure;
using Signify.PAD.Svc.Core.Models;
using Signify.PAD.Svc.Core.Services;
using Signify.PAD.Svc.Core.Services.Interfaces;
using Signify.PAD.Svc.Core.Validators;
using System;
using System.IO.Abstractions;
using System.Net.Http;

namespace Signify.PAD.Svc.Core.DI;

public static class ServiceCollectionHelper
{
    public static void AddCoreConfigs(this IServiceCollection services, IConfiguration config)
    {
        AddConfigs(services, config);

        AddPadServices(services);

        services.AddSingleton(AutoMapperConfig.AddAutoMapper(services));
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        services.AddDbContext<PADDataContext>(opts => opts.UseNpgsql(config.GetConnectionString("DB")));

        services.AddSingleton(NewRelic.Api.Agent.NewRelic.GetAgent());

        AddRefitClients(services);

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
                if (streamingConfig.SecurityProtocol?.ToLower() == "sasl_ssl")
                {
                    consumerOptions.ConfigurationOptions["security.protocol"] = streamingConfig.SecurityProtocol;
                    consumerOptions.ConfigurationOptions["sasl.mechanism"] = streamingConfig.Mechanism;
                    consumerOptions.ConfigurationOptions["sasl.username"] = streamingConfig.Username;
                    consumerOptions.ConfigurationOptions["sasl.password"] = streamingConfig.Password;
                }

                consumerOptions.SubscribeTo("evaluation", "pdfdelivery", "cdi_events", "rcm_bill");
                consumerOptions.AddAssemblyFromType<EvaluationFinalizedHandler>();
                consumerOptions.ContinueOnFailure = streamingConfig.ContinueOnFailure;
                consumerOptions.ContinueOnDeserializationErrors = services.BuildServiceProvider().GetRequiredService<IFeatureFlags>().EnableDlq;
            },
            setupProducer: producerOptions =>
            {
                producerOptions.KafkaBrokers = streamingConfig.KafkaBrokers;
                producerOptions.ProducerInstances = streamingConfig.ProducerInstances;
                producerOptions.SetTopicResolver(@event =>
                {
                    return @event switch
                    {
                        Performed => "PAD_Status",
                        NotPerformed => "PAD_Status",
                        BillRequestSent => "PAD_Status",
                        BillRequestNotSent => "PAD_Status",
                        ProviderPayRequestSent => "PAD_Status",
                        // This has to be placed before call to ProviderPayableEventReceived as ProviderNonPayableEventReceived is derived from ProviderPayableEventReceived
                        ProviderNonPayableEventReceived => "PAD_Status",
                        ProviderPayableEventReceived => "PAD_Status",
                        ResultsReceived => "PAD_Results",
                        AoeResult => "pad_clinical_support",
                        EvaluationDlqMessage => "dps_evaluation_dlq",
                        PdfDeliveryDlqMessage => "dps_pdfdelivery_dlq",
                        CdiEventDlqMessage => "dps_cdi_events_dlq",
                        RcmBillDlqMessage => "dps_rcm_bill_dlq",
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

        AddMediatr(services);

        RegisterCustomHealthChecks(services, config);
    }

    private static void AddConfigs(IServiceCollection services, IConfiguration config)
    {
        AddConfig<WebApiConfig, WebApiConfig>("ApiUrls");
        AddConfig<OktaConfig, OktaConfig>("Okta");
        AddConfig<ServiceBusConfig, ServiceBusConfig>("ServiceBus");
        AddConfig<LaunchDarklyConfig, LaunchDarklyConfig>("LaunchDarkly");

        AddConfig<WaveformConfig, WaveformConfig>("Waveform");
        services.AddSingleton<IWaveformBackgroundServiceConfig>(sp => sp.GetRequiredService<WaveformConfig>());
        services.AddSingleton<IWaveformDirectoryConfig>(sp => sp.GetRequiredService<WaveformConfig>());
        services.AddSingleton<IWaveformThresholdConfig>(sp => sp.GetRequiredService<WaveformConfig>());
        services.AddSingleton<IWaveformVendorsConfig>(sp => sp.GetRequiredService<WaveformConfig>());

        AddConfig<WaveformReProcessConfig, WaveformReProcessConfig>("WaveformReProcess");
        services.AddSingleton<IWaveformReProcessConfig>(sp => sp.GetRequiredService<WaveformReProcessConfig>());
        return;

        void AddConfig<TConfig, TImplementation>(string section, bool isOptional = false, Action<TImplementation> configAction = null)
            where TImplementation : class, TConfig, new()
        {
            var subsection = isOptional ? config.GetSection(section) : config.GetRequiredSection(section);
            if (isOptional && subsection.Value == null)
                return;

            var tConfig = subsection.Get<TImplementation>(opts => opts.BindNonPublicProperties = true) ?? new TImplementation();

            configAction?.Invoke(tConfig);

            services.AddSingleton(typeof(TConfig), tConfig);
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
            .AddDbContextCheck<PADDataContext>(customTestQuery: async (ctx, cancellationToken) =>
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

    private static void AddRefitClients(IServiceCollection services)
    {
        var isLocal = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Local";
        services.AddMemoryCache();

        services.AddScoped<OktaClientCredentialsHttpClientHandler>();

        if (isLocal)
        {
            services.AddRefitClient<IEvaluationApi>()
                .ConfigureHttpClient((provider, client) =>
                {
                    var config = provider.GetRequiredService<WebApiConfig>();
                    client.BaseAddress = config.EvaluationApiUrl;
                });
        }
        else
        {
            services.AddRefitClient<IEvaluationApi>(new RefitSettings(new NewtonsoftJsonContentSerializer()))
                .ConfigureHttpClient((sp, c) =>
                {
                    var config = sp.GetRequiredService<WebApiConfig>();
                    c.BaseAddress = config.EvaluationApiUrl;
                }).AddHttpMessageHandler(s => s.GetService<OktaClientCredentialsHttpClientHandler>());
        }

        if (isLocal)
        {
            services.AddRefitClient<IProviderApi>()
                .ConfigureHttpClient((provider, client) =>
                {
                    var config = provider.GetRequiredService<WebApiConfig>();
                    client.BaseAddress = config.ProviderApiUrl;
                });
        }
        else
        {
            services.AddRefitClient<IProviderApi>(new RefitSettings(new NewtonsoftJsonContentSerializer()))
                .ConfigureHttpClient((sp, c) =>
                {
                    var config = sp.GetRequiredService<WebApiConfig>();
                    c.BaseAddress = config.ProviderApiUrl;
                }).AddHttpMessageHandler(s => s.GetService<OktaClientCredentialsHttpClientHandler>());
        }

        if (isLocal)
        {
            services.AddRefitClient<IMemberInfoApi>()
                .ConfigureHttpClient((provider, client) =>
                {
                    var config = provider.GetRequiredService<WebApiConfig>();
                    client.BaseAddress = config.MemberApiUrl;
                });
        }
        else
        {
            services.AddRefitClient<IMemberInfoApi>(new RefitSettings(new NewtonsoftJsonContentSerializer()))
                .ConfigureHttpClient((sp, c) =>
                {
                    var config = sp.GetRequiredService<WebApiConfig>();
                    c.BaseAddress = config.MemberApiUrl;
                }).AddHttpMessageHandler(s => s.GetService<OktaClientCredentialsHttpClientHandler>());
        }

        services.AddRefitClient<IOktaApi>(new RefitSettings(new NewtonsoftJsonContentSerializer()))
            .ConfigureHttpClient((sp, c) =>
            {
                var config = sp.GetRequiredService<OktaConfig>();
                c.BaseAddress = config.Domain;
            });

        if (isLocal)
        {
            services.AddRefitClient<IRcmApi>()
                .ConfigureHttpClient((provider, client) =>
                {
                    var config = provider.GetRequiredService<WebApiConfig>();
                    client.BaseAddress = config.RcmApiUrl;
                });
        }
        else
        {
            services.AddRefitClient<IRcmApi>(new RefitSettings(new NewtonsoftJsonContentSerializer()))
                .ConfigureHttpClient((sp, c) =>
                {
                    var config = sp.GetRequiredService<WebApiConfig>();
                    c.BaseAddress = config.RcmApiUrl;
                }).AddHttpMessageHandler(s => s.GetService<OktaClientCredentialsHttpClientHandler>());
        }

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
    }

    private static void AddPadServices(IServiceCollection services)
    {
        services.AddSingleton<IFeatureFlags, FeatureFlags>();
        services.AddSingleton<IApplicationTime, ApplicationTime>();
        services.AddSingleton<IDateTimeValidator, DateTimeValidator>();
        services.AddSingleton<IStringValidator, StringValidator>();
        services.AddSingleton<IWaveformDocumentConverter, WaveformDocumentConverter>();
        services.AddSingleton<IProductFilter, ProductFilter>();
        services.AddSingleton<IBuildAnswerLookup, AnswerLookupBuilderService>();
        services.AddSingleton<BillAndPayRules>();
        services.AddSingleton<IPayableRules>(sp => sp.GetRequiredService<BillAndPayRules>());
        services.AddSingleton<IBillableRules>(sp => sp.GetRequiredService<BillAndPayRules>());

        services.AddScoped<ITransactionSupplier, TransactionSupplier>();

        AddFileDirectoryServices(services);

        // Yes, the above services (among others, such as Mediatr command handlers) that still have to do
        // with waveforms are still being registered, but I can't think of a better way of doing this now.
        // Command handlers are registered via a generic `services.AddMediatr(typeof(CreateOrUpdatePADHandler).GetTypeInfo().Assembly);`
        // call above, which adds all handlers within the assembly, and we have no way of specifying some but not
        // others. And same when it comes to the NSB event handlers. So if the above services are not registered,
        // but the Mediatr and NSB handlers _are_ registered, the startup throws DI exceptions. I think this is the
        // best we can do as of now - just don't register the background services that kick off all the waveform
        // processing, but leave the command/event handlers as registered.

        using var sp = services.BuildServiceProvider();

        var config = sp.GetRequiredService<WaveformReProcessConfig>();

        if (config.IsEnabled) // To avoid the re-processing service affecting the Pending service, only run one or the other
        {
            services.AddHostedService<WaveformReProcessService>();
        }
        else
        {
            services.AddHostedService<WaveformPendingService>();
            services.AddHostedService<WaveformIncomingService>();
        }
    }

    private static void AddFileDirectoryServices(IServiceCollection services)
    {
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IDirectoryServices, DirectoryServices>();
    }
}
