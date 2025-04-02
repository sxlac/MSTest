using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.Dps.Observability.Library.Services;
using Signify.PAD.Svc.Core.Configs.WaveformConfigs;
using Signify.PAD.Svc.Core.Constants;
using Signify.PAD.Svc.Core.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace Signify.PAD.Svc.Core.Services;

/// <summary>
/// Simple service to move files from the Incoming directory to the Pending directory
/// </summary>
public sealed class WaveformIncomingService : WaveformBackgroundServiceBase
{
    public WaveformIncomingService(ILogger<WaveformIncomingService> logger, IServiceScopeFactory serviceScopeFactory, IWaveformBackgroundServiceConfig waveformConfig)
        : base(logger, serviceScopeFactory, waveformConfig)
    {
    }

    [Transaction]
    protected override Task Execute(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var directoryService = serviceProvider.GetRequiredService<IDirectoryServices>();
        var config = serviceProvider.GetRequiredService<IWaveformVendorsConfig>();
        var observabilityService = serviceProvider.GetRequiredService<IObservabilityService>();

        foreach (var vendor in config.VendorConfigs)
        {
            try
            {
                MoveFilesToPending(observabilityService, vendor, directoryService);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed processing for vendor {Vendor}", vendor.VendorName);
            }
        }

        return Task.CompletedTask;
    }

    private void MoveFilesToPending(IObservabilityService observabilityService, IWaveformVendorConfig vendor, IDirectoryServices directoryService)
    {
        var incomingDir = directoryService.GetIncomingDirectory(vendor.VendorDirectory);
        var pendingDir = directoryService.GetPendingDirectory(vendor.VendorDirectory);
        var failedDir = directoryService.GetFilePendingAlreadyDirectory(vendor.VendorDirectory);

        var files = directoryService.GetFilesFromDirectory(incomingDir).ToList();
        if (!files.Any())
        {
            Logger.LogInformation("No new files from vendor {Vendor} to process", vendor.VendorName);
            return;
        }

        var successCount = 0;
        foreach (var file in files)
        {
            if (!directoryService.MoveFileDirectory(incomingDir, pendingDir, file))
            {
                directoryService.MoveFileDirectory(incomingDir, failedDir, file, true);
                Logger.LogWarning("Failed to move file from {Incoming} to {Pending}", incomingDir, pendingDir);
            }
            else
            {
                ++successCount;
            }
        }

        var level = successCount == files.Count ? LogLevel.Information : LogLevel.Warning;
        
        observabilityService.AddEvent(Observability.Waveform.WaveformDocumentIncomingEvent, new Dictionary<string, object>()
        {
            {"IncomingSuccessCount", successCount},
            {"IncomingFailedCount", files.Count - successCount}
        });
        
        Logger.Log(level, "Moved {SuccessCount} out of {TotalCount} files from incoming to pending for vendor {Vendor}",
            successCount, files.Count, vendor.VendorName);
    }
}