using Signify.PAD.Svc.Core.Configs.WaveformConfigs;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Signify.PAD.Svc.Core.DI.Configs;

[ExcludeFromCodeCoverage]
public class WaveformConfig :
    IWaveformBackgroundServiceConfig,
    IWaveformDirectoryConfig,
    IWaveformVendorsConfig,
    IWaveformThresholdConfig
{
    #region IWaveformBackgroundServiceConfig
    /// <inheritdoc />
    public int PollingPeriodSeconds { get; set; }
    #endregion IWaveformBackgroundServiceConfig

    #region IWaveformDirectoryConfig
    public string ParentDirectory { get; set; }
    /// <inheritdoc />
    public string RootDirectoryPath => ParentDirectory;
    /// <inheritdoc />
    public string IncomingDirectory { get; set; }
    /// <inheritdoc />
    public string PendingDirectory { get; set; }
    /// <inheritdoc />
    public string ProcessedDirectory { get; set; }
    /// <inheritdoc />
    public string IgnoredDirectory { get; set; }
    public WaveformFailedDirectoryConfig FailedDirectory { get; set; }
    /// <inheritdoc />
    public IWaveformFailedDirectoryConfig FailedDirectoryConfig => FailedDirectory;
    #endregion IWaveformDirectoryConfig

    #region IWaveformVendorConfig
    public IEnumerable<WaveformVendorConfig> Vendors { get; set; }
    /// <inheritdoc />
    public IEnumerable<IWaveformVendorConfig> VendorConfigs => Vendors;
    #endregion IWaveformVendorConfig

    #region IWaveformThresholdConfigs
    /// <inheritdoc />
    public int FileAgeThresholdDays { get; init; }
    #endregion IWaveformThresholdConfigs
}
