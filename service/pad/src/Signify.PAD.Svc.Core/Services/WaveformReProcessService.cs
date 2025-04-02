using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Configs.WaveformConfigs;
using Signify.PAD.Svc.Core.Data;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Models;
using Signify.PAD.Svc.Core.Queries;
using Signify.PAD.Svc.Core.Services.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Pad = Signify.PAD.Svc.Core.Data.Entities.PAD;

namespace Signify.PAD.Svc.Core.Services;

public class WaveformReProcessService : WaveformBackgroundServiceBase
{
    private readonly ILogger _logger;

    public WaveformReProcessService(ILogger<WaveformReProcessService> logger,
        IServiceScopeFactory serviceScopeFactory,
        IWaveformBackgroundServiceConfig config)
        : base(logger, serviceScopeFactory, config)
    {
        _logger = logger;
    }

    [Transaction]
    protected override async Task Execute(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var config = serviceProvider.GetRequiredService<IWaveformReProcessConfig>();

        var vendor = serviceProvider.GetRequiredService<IWaveformVendorsConfig>()
            .VendorConfigs
            .Single(each => each.VendorName == config.VendorName);

        _logger.LogInformation("Starting waveform re-processing service, looking for waveforms from Vendor={Vendor} created between {StartDateTime} and {EndDateTime}",
            vendor.VendorName, config.StartDateTime, config.EndDateTime);

        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var waveforms = await mediator.Send(new QueryWaveforms(vendor.VendorName, config.StartDateTime, config.EndDateTime), cancellationToken);
        if (waveforms.Count < 1)
        {
            _logger.LogInformation("No waveforms found, nothing to do");
            return;
        }

        var directoryServices = serviceProvider.GetRequiredService<IDirectoryServices>();
        var transactionSupplier = serviceProvider.GetRequiredService<ITransactionSupplier>();

        var successCount = 0;

        foreach (var waveform in waveforms)
        {
            if (await SetWaveformToBeReprocessed(mediator, transactionSupplier, vendor, directoryServices, waveform, cancellationToken))
                ++successCount;
        }

        _logger.Log(successCount == waveforms.Count ? LogLevel.Information : LogLevel.Warning,
            "Successfully set {SuccessCount} out of {TotalCount} waveforms for Vendor={Vendor} to be re-processed", successCount, waveforms.Count, vendor.VendorName);
    }

    private async Task<bool> SetWaveformToBeReprocessed(IMediator mediator, ITransactionSupplier transactionSupplier,
        IWaveformVendorConfig vendor, IDirectoryServices directoryServices, WaveformDocument waveform, CancellationToken cancellationToken)
    {
        using var transaction = transactionSupplier.BeginTransaction();

        try
        {
            var pad = await mediator.Send(new GetPadByMemberPlanId(waveform.MemberPlanId, waveform.DateOfExam), cancellationToken);

            await mediator.Send(new DeleteWaveformDocument(waveform), cancellationToken);

            await mediator.Send(new DeleteStatuses(pad.PADId, [StatusCodes.WaveformDocumentDownloaded, StatusCodes.WaveformDocumentUploaded]), cancellationToken);

            if (!MoveFile(directoryServices, vendor, waveform, pad))
            {
                _logger.LogWarning("Failed to move WaveformDocumentId={WaveformDocumentId} from Vendor={Vendor} from the processed to pending directory", waveform.WaveformDocumentId, vendor.VendorName);
                return false;
            }

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Successfully set WaveformDocumentId={WaveformDocumentId} for Vendor={Vendor} to be processed again", waveform.WaveformDocumentId, vendor.VendorName);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set WaveformDocumentId={WaveformDocumentId} for Vendor={Vendor} to be re-processed", waveform.WaveformDocumentId, vendor.VendorName);

            return false;
        }
    }

    private static bool MoveFile(IDirectoryServices directoryServices, IWaveformVendorConfig vendor, WaveformDocument waveform, Pad pad)
    {
        var processedDir = directoryServices.GetProcessedDirectory(vendor.VendorDirectory, pad.ClientId, waveform.CreatedDateTime);

        var pendingDir = directoryServices.GetPendingDirectory(vendor.VendorDirectory);

        return directoryServices.MoveFileDirectory(processedDir, pendingDir, waveform.Filename);
    }
}
