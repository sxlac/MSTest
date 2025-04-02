using System;

namespace Signify.PAD.Svc.Core.Configs.WaveformConfigs;

/// <summary>
/// Configuration around re-processing waveform pdf files
/// </summary>
public interface IWaveformReProcessConfig
{
    /// <summary>
    /// Starting timestamp for when the waveforms were previously processed, which now need to be processed again 
    /// </summary>
    /// <remarks>Inclusive</remarks>
    public DateTime StartDateTime { get; }
    /// <summary>
    /// Ending timestamp for when the waveforms were previously processed, which now need to be processed again 
    /// </summary>
    /// <remarks>Inclusive</remarks>
    public DateTime EndDateTime { get; }
    /// <summary>
    /// Name of the vendor whose waveforms need to be re-processed
    /// </summary>
    public string VendorName { get; }
}
