namespace Signify.PAD.Svc.Core.Configs.WaveformConfigs;

public interface IWaveformFailedDirectoryConfig
{
    string FailedRootDirectoryPath { get; }
    string FileAlreadyUploadedDirectory { get; }
    string FileAlreadyInPendingDirectory { get; }
    string FileOlderThanThreshold { get; }
}

public class WaveformFailedDirectoryConfig : IWaveformFailedDirectoryConfig
{
    public string FailedParentDirectory { get; set; }
    public string FailedRootDirectoryPath => FailedParentDirectory;
    public string FileAlreadyUploadedDirectory { get; set; }
    public string FileAlreadyInPendingDirectory { get; set; }
    public string FileOlderThanThreshold { get; set; }
}
