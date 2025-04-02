using AutoMapper;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Signify.Tools.MessageQueue.Core.Interfaces;
using Signify.Tools.MessageQueue.Maps;
using Signify.Tools.MessageQueue.Services;
using Signify.Tools.MessageQueue.Services.Interfaces;
using Signify.Tools.MessageQueue.Settings;
using Serilog;
using System.Reflection;
using Signify.Tools.MessageQueue.Core.CSV;
using Signify.Tools.MessageQueue.Queue;
using Signify.Tools.MessageQueue.Queue.Interfaces;

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((context, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithThreadId())
    .ConfigureServices((context, services) =>
    {
        services.AddMediatR(typeof(NServiceBusService).GetTypeInfo().Assembly);

        var config = context.Configuration;

        #region Configs
        services.AddSingleton(Options.Create(config.GetSection("NServiceBusSettings").Get<NServiceBusSettings>()));
        services.AddSingleton(Options.Create(config.GetSection("ConnectionStrings").Get<ConnectionStringSettings>()));
        services.AddSingleton(Options.Create(config.GetSection("CsvSettings").Get<CsvSettings>()));
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

        // Register singletons below
        services.AddSingleton(sp => new MessagesCsvFileReader(sp.GetRequiredService<ILogger<MessagesCsvFileReader>>(),
                                                                sp.GetRequiredService<IOptions<CsvSettings>>()))
            .AddSingleton<IMessagesCsvFileReader>(sp => sp.GetRequiredService<MessagesCsvFileReader>());

        services.AddSingleton(sp => new TemplateCsvFileWriter(sp.GetRequiredService<ILogger<TemplateCsvFileWriter>>(),
                                                                sp.GetRequiredService<IOptions<CsvSettings>>(),
                                                                sp.GetRequiredService<IOptions<NServiceBusSettings>>()))
            .AddSingleton<ITemplateCsvFileWriter>(sp => sp.GetRequiredService<TemplateCsvFileWriter>());

        services.AddSingleton(sp => new MessengerService(sp.GetRequiredService<ILogger<MessengerService>>(),
                                                            sp.GetRequiredService<IMessagesCsvFileReader>(),
                                                            sp.GetRequiredService<IOptions<NServiceBusSettings>>(),
                                                            sp.GetRequiredService<IOptions<ConnectionStringSettings>>()))
            .AddSingleton<IMessengerService>(sp => sp.GetRequiredService<MessengerService>());

        services.AddSingleton(sp => new SendMessageManager(sp.GetRequiredService<ILogger<SendMessageManager>>(),
                                                            sp.GetRequiredService<IMessengerService>()))
            .AddSingleton<ISendMessageManager>(sp => sp.GetRequiredService<SendMessageManager>());

        services.AddSingleton(sp => new TemplateFileService(sp.GetRequiredService<ILogger<TemplateFileService>>(),
                                                                sp.GetRequiredService<ITemplateCsvFileWriter>()))
            .AddSingleton<ITemplateFileService>(sp => sp.GetRequiredService<TemplateFileService>());

        // Register long-running background service below
        services.AddHostedService(sp => new NServiceBusService(
            sp.GetRequiredService<ILogger<NServiceBusService>>(),
            sp.GetRequiredService<ISendMessageManager>(),
            sp.GetRequiredService<ITemplateFileService>(),
            sp.GetRequiredService<IOptions<NServiceBusSettings>>()));

        #endregion Services
    })
    .Build();

await host.RunAsync();