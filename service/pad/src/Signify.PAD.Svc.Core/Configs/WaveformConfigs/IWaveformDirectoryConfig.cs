namespace Signify.PAD.Svc.Core.Configs.WaveformConfigs;

public interface IWaveformDirectoryConfig
{
    /// <summary>
    /// Path to the root directory for waveform file processing
    /// </summary>
    string RootDirectoryPath { get; }
    /// <summary>
    /// Name of the directory where "incoming" waveform files will be placed (ie
    /// where the files are placed before any handling by this process manager starts)
    /// </summary>
    string IncomingDirectory { get; }
    /// <summary>
    /// Name of the directory where waveform files are staged once picked up from the
    /// <see cref="IncomingDirectory"/>
    /// process
    /// </summary>
    /// <remarks>
    /// <see cref="IncomingDirectory"/> --> <see cref="PendingDirectory"/> --> &lt;processing starts&gt;
    /// --> <see cref="ProcessedDirectory"/> OR <see cref="IgnoredDirectory"/>
    /// </remarks>
    string PendingDirectory { get; }
    /// <summary>
    /// Name of the directory where waveform files are moved if and once successful
    /// processing is complete
    /// </summary>
    string ProcessedDirectory { get; }
    /// <summary>
    /// Name of the directory where waveform files are moved if they are ignored and
    /// not processed
    /// </summary>
    string IgnoredDirectory { get; }
    /// <summary>
    /// Configuration for waveform files that fail to be processed
    /// </summary>
    IWaveformFailedDirectoryConfig FailedDirectoryConfig { get; }
}