namespace Signify.PAD.Svc.Core.Configs.WaveformConfigs;

/// <summary>
/// Threshold configurations for the processing of Waveform pdf documents
/// </summary>
public interface IWaveformThresholdConfig
{
    /// <summary>
    /// Waveform documents older than this are ignored
    /// </summary>
    public int FileAgeThresholdDays { get; }
}
