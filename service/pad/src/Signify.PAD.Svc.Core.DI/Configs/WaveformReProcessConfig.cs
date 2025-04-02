using Signify.PAD.Svc.Core.Configs.WaveformConfigs;
using System;

namespace Signify.PAD.Svc.Core.DI.Configs;

/// <summary>
/// Configuration around re-processing waveform pdf files
/// </summary>
public class WaveformReProcessConfig : IWaveformReProcessConfig
{
    /// <summary>
    /// Whether to enable the waveform pdf re-processing service
    /// </summary>
    public bool IsEnabled { get; init; }
    /// <inheritdoc />
    public DateTime StartDateTime { get; init; }
    /// <inheritdoc />
    public DateTime EndDateTime { get; init; }
    /// <inheritdoc />
    public string VendorName { get; init; }
}
