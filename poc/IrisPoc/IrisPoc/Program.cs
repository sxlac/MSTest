using AutoMapper;
using IrisPoc.Maps;
using IrisPoc.Services;
using IrisPoc.Services.Image;
using IrisPoc.Services.IO;
using IrisPoc.Services.Orders;
using IrisPoc.Services.Storage;
using IrisPoc.Settings;
using Microsoft.Extensions.Options;
using Serilog;

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((context, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithThreadId())
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;

        #region Configs
        // Unlike with the IRIS API, when using the service bus, the Site LocalId is a required field 
        services.AddSingleton(Options.Create(config.GetSection("Iris").Get<IrisSettings>()));

        // I've created an optional Startup section, allowing you to place orders on startup
        services.AddSingleton(Options.Create(config.GetSection("Startup").Get<StartupSettings>()));
        #endregion Configs

        #region AutoMapper
        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddProfile<MappingProfile>();
            mc.ConstructServicesUsing(type =>
                ActivatorUtilities.CreateInstance(services.BuildServiceProvider(), type));
        });

        services.AddSingleton(mappingConfig.CreateMapper());
        #endregion AutoMapper

        #region Services
        services.AddSingleton<ImageService>()
            .AddSingleton<IImageService>(sp => sp.GetRequiredService<ImageService>());

        services.AddSingleton<FileService>()
            .AddSingleton<IFileService>(sp => sp.GetRequiredService<FileService>());

        services.AddSingleton(sp => new OrderSubmissionServiceWrapper(
                sp.GetRequiredService<ILogger<OrderSubmissionServiceWrapper>>(),
                config.GetConnectionString("IrisOrderSubmission"),
                sp.GetRequiredService<IOptions<StartupSettings>>(),
                sp.GetRequiredService<IMapper>(),
                sp.GetRequiredService<IImageService>()))
            .AddSingleton<IOrderSubmissionService>(sp => sp.GetRequiredService<OrderSubmissionServiceWrapper>());

        services.AddSingleton(sp => new AzureBlobStorageService(
                sp.GetRequiredService<ILogger<AzureBlobStorageService>>(),
                config.GetConnectionString("BlobStorage")))
            .AddSingleton<IBlobStorageService>(sp => sp.GetRequiredService<AzureBlobStorageService>());

        services.AddSingleton(sp => new IrisStorageService(
                sp.GetRequiredService<ILogger<IrisStorageService>>(),
                config.GetConnectionString("BlobStorage"),
                sp.GetRequiredService<IFileService>()))
            .AddSingleton<IIrisStorageService>(sp => sp.GetRequiredService<IrisStorageService>());

        // Register a background service that will submit orders to IRIS via the service bus on application startup
        services.AddHostedService<OrderSubmissionBackgroundService>();

        // Register a long-running background service for subscribing to order results
        services.AddHostedService(sp => new OrderResultsBackgroundService(
            sp.GetRequiredService<ILogger<OrderResultsBackgroundService>>(),
            config.GetConnectionString("IrisResultDelivery"),
            sp.GetRequiredService<IOptions<StartupSettings>>()));
        #endregion Services
    })
    .Build();

await host.RunAsync();
