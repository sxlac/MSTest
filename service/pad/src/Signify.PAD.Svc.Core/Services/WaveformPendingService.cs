using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Configs.WaveformConfigs;
using Signify.PAD.Svc.Core.Constants;
using Signify.PAD.Svc.Core.Infrastructure;
using Signify.PAD.Svc.Core.Queries;
using Signify.PAD.Svc.Core.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Signify.PAD.Svc.Core.Services;

/// <summary>
/// Iterates through files in each vendor's Pending directory, and sends them for processing
/// </summary>
public sealed class WaveformPendingService : WaveformBackgroundServiceBase
{
    public WaveformPendingService(ILogger<WaveformPendingService> logger,
        IServiceScopeFactory serviceScopeFactory,
        IWaveformBackgroundServiceConfig waveformConfig)
        : base(logger, serviceScopeFactory, waveformConfig)
    {
    }

    protected override async Task Execute(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var directoryService = serviceProvider.GetRequiredService<IDirectoryServices>();
        var applicationTime = serviceProvider.GetRequiredService<IApplicationTime>();
        var config = serviceProvider.GetRequiredService<IWaveformVendorsConfig>();
        var thresholdConfig = serviceProvider.GetRequiredService<IWaveformThresholdConfig>();
        var observability = serviceProvider.GetRequiredService<IObservabilityService>();

        var fileAgeService = new FileAgeService(directoryService, applicationTime, thresholdConfig);

        foreach (var vendor in config.VendorConfigs)
        {
            try
            {
                await ProcessPendingFiles(observability, vendor, directoryService, mediator, fileAgeService, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to process pending files for vendor {Vendor}", vendor.VendorName);
            }
        }
    }

    [Transaction]
    private async Task ProcessPendingFiles(IObservabilityService observability, IWaveformVendorConfig vendorConfig, IDirectoryServices directoryService, ISender mediator, FileAgeService fileAgeService, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Processing pending files for vendor {Vendor}", vendorConfig.VendorName);

        var files = directoryService.GetFilesFromDirectory(directoryService.GetPendingDirectory(vendorConfig.VendorDirectory)).ToList();
        if (!files.Any())
        {
            Logger.LogInformation("No pending files from vendor {Vendor} to process", vendorConfig.VendorName);
            return;
        }

        var oldFiles = 0;
        var testCount = 0;
        var alreadyUploaded = 0;
        var failedToMoveFile = 0;
        var successCount = 0;
        foreach (var file in files)
        {
            var result = await ProcessPendingFile(vendorConfig, directoryService, mediator, fileAgeService, file, cancellationToken);

            switch (result)
            {
                case 1:
                    ++oldFiles;
                    break;

                case 2:
                    ++testCount;
                    break;

                case 3:
                    ++alreadyUploaded;
                    break;

                case 4:
                    ++failedToMoveFile;
                    break;

                case 5:
                    ++successCount;
                    break;
            }
        }

        observability.AddEvent(Observability.Waveform.WaveformDocumentPendingEvent, new Dictionary<string, object>
        {
            {"TotalCount", files.Count},
            {"SuccessCount", successCount},
            {"FailedCount", failedToMoveFile},
            {"FilesRemaining", files.Count - (oldFiles + testCount + alreadyUploaded + successCount)},
            {"OldFilesCount", oldFiles},
            {"AlreadyUploaded", alreadyUploaded},
            {"TestFailedCount" /*are these really "failed"?*/, testCount}
        });

        var processedCount = successCount + alreadyUploaded + oldFiles + testCount;

        Logger.Log(processedCount == files.Count ? LogLevel.Information : LogLevel.Warning,
            "Successfully processed {ProcessedCount} out of {TotalCount} pending files for vendor {Vendor}; " +
            "SuccessCount={SuccessCount}, AlreadyUploadedCount={AlreadyUploadedCount}, OldFileCount={OldFileCount}...",
            processedCount, files.Count, vendorConfig.VendorName,
            successCount, alreadyUploaded, oldFiles);
    }

    private async Task<int> ProcessPendingFile(IWaveformVendorConfig vendorConfig, IDirectoryServices directoryService, ISender mediator, FileAgeService fileAgeService, string file, CancellationToken cancellationToken)
    {
        var vendor = await mediator.Send(new GetWaveformDocumentVendorByName
        {
            WaveformDocumentVendorName = vendorConfig.VendorName
        }, cancellationToken);

        var pendingDir = directoryService.GetPendingDirectory(vendorConfig.VendorDirectory);
        var filePath = pendingDir + file;

        if (fileAgeService.IsBelowThreshold(filePath))
        {
            var oldFileDirectory = directoryService.GetFileOlderThanThresholdDirectory(vendorConfig.VendorDirectory);
            if (!directoryService.MoveFileDirectory(pendingDir, oldFileDirectory, file, true))
            {
                Logger.LogWarning("Failed to move file from {Pending} to {OldFile}", pendingDir, oldFileDirectory);
                return 4;
            }
            return 1;
        }

        var result = await mediator.Send(new ProcessPendingWaveform
        {
            Vendor = vendor,
            Filename = file,
            FilePath = filePath
        }, cancellationToken);

        switch (result)
        {
            case { IgnoreFile: true }:
                if (!directoryService.DeleteFile(pendingDir, file))
                {
                    Logger.LogWarning("Failed to delete file");
                    return 4;
                }
                return 2;

            case { FileAlreadyUploaded: true }:
                var failedDirectory = directoryService.GetFileUploadedAlreadyDirectory(vendorConfig.VendorDirectory);
                if (!directoryService.MoveFileDirectory(pendingDir, failedDirectory, file, true))
                {
                    Logger.LogWarning("Failed to move file from {Pending} to {Failed}", pendingDir, failedDirectory);
                    return 4;
                }
                return 3;
            
            case { IsSuccessful: false }:
                return 0;

            default:
                var processedDir = directoryService.GetProcessedDirectory(vendorConfig.VendorDirectory, result.ClientId);
                if (!directoryService.MoveFileDirectory(pendingDir, processedDir, file))
                {
                    Logger.LogWarning("Failed to move file from {Pending} to {Processed}", pendingDir, processedDir);
                    return 4;
                }
                break;
        }
        return 5;
    }

    private class FileAgeService
    {
        private readonly IDirectoryServices _directoryService;
        private readonly DateTime _threshold;

        public FileAgeService(IDirectoryServices directoryService, IApplicationTime applicationTime, IWaveformThresholdConfig config)
        {
            _directoryService = directoryService;
            _threshold = applicationTime
                .UtcNow()
                .AddDays(-1 * Math.Abs(config.FileAgeThresholdDays)); // should be positive in config, but just in case, taking ABS
        }

        /// <summary>
        /// Checks whether the given file's age is within the configured threshold to be processed
        /// </summary>
        /// <param name="filename"></param>
        /// <returns><c>true</c> if it should be processed; <c>false</c> if the file should be ignored</returns>
        public bool IsBelowThreshold(string filename)
            => _directoryService.GetCreationTimeUtc(filename) <= _threshold;
    }
}
