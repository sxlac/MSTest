using Microsoft.Extensions.Logging;
using Signify.PAD.Svc.Core.Configs.WaveformConfigs;
using Signify.PAD.Svc.Core.Infrastructure;
using Signify.PAD.Svc.Core.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

namespace Signify.PAD.Svc.Core.Services;

public class DirectoryServices : IDirectoryServices
{
    private readonly ILogger _logger;
    private readonly IWaveformDirectoryConfig _waveformConfig;
    private readonly IFileSystem _fileSystem;
    private readonly IApplicationTime _applicationTime;

    public DirectoryServices(ILogger<DirectoryServices> logger,
        IWaveformDirectoryConfig waveformConfig,
        IFileSystem fileSystem,
        IApplicationTime applicationTime)
    {
        _logger = logger;
        _waveformConfig = waveformConfig;
        _fileSystem = fileSystem;
        _applicationTime = applicationTime;
    }

    public string GetIncomingDirectory(string vendorDirectory) => _fileSystem.Path.Join(_waveformConfig.RootDirectoryPath, _waveformConfig.IncomingDirectory, vendorDirectory);

    public string GetPendingDirectory(string vendorDirectory) => _fileSystem.Path.Join(_waveformConfig.RootDirectoryPath, _waveformConfig.PendingDirectory, vendorDirectory);

    public string GetIgnoredDirectory(string vendorDirectory) => _fileSystem.Path.Join(_waveformConfig.RootDirectoryPath, _waveformConfig.IgnoredDirectory, vendorDirectory);

    public string GetFileUploadedAlreadyDirectory(string vendorDirectory) => _fileSystem.Path.Join(_waveformConfig.RootDirectoryPath, _waveformConfig.FailedDirectoryConfig.FailedRootDirectoryPath, _waveformConfig.FailedDirectoryConfig.FileAlreadyUploadedDirectory, vendorDirectory);
    
    public string GetFileOlderThanThresholdDirectory(string vendorDirectory) => _fileSystem.Path.Join(_waveformConfig.RootDirectoryPath, _waveformConfig.FailedDirectoryConfig.FailedRootDirectoryPath, _waveformConfig.FailedDirectoryConfig.FileOlderThanThreshold, vendorDirectory);

    public string GetFilePendingAlreadyDirectory(string vendorDirectory) => _fileSystem.Path.Join(_waveformConfig.RootDirectoryPath, _waveformConfig.FailedDirectoryConfig.FailedRootDirectoryPath, _waveformConfig.FailedDirectoryConfig.FileAlreadyInPendingDirectory, vendorDirectory);

    private string BaseProcessedDirectory(string vendorDirectory) => _fileSystem.Path.Join(_waveformConfig.RootDirectoryPath, _waveformConfig.ProcessedDirectory, vendorDirectory, "ClientId");
    public string GetProcessedDirectory(string vendorDirectory, int? clientId)
        => GetProcessedDirectory(vendorDirectory, clientId, _applicationTime.UtcNow());

    /// <inheritdoc />
    public string GetProcessedDirectory(string vendorDirectory, int? clientId, DateTimeOffset processDateTime)
        => _fileSystem.Path.Join(BaseProcessedDirectory(vendorDirectory), clientId.ToString(), processDateTime.Year.ToString(), processDateTime.Month.ToString());

    public IEnumerable<string> GetFilesFromDirectory(string directory)
    {
        try
        {
            return _fileSystem.Directory.EnumerateFiles(directory, "*.pdf", new EnumerationOptions
            {
                MatchCasing = MatchCasing.CaseInsensitive
            }).Select(_fileSystem.Path.GetFileName);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to get files from directory {Directory} due to an error: {Message}", directory, e.Message);
        }
        return Enumerable.Empty<string>();
    }

    public bool MoveFileDirectory(string source, string target, string file, bool failed = false)
    {
        try
        {
            if (!_fileSystem.Directory.Exists(target))
                _fileSystem.Directory.CreateDirectory(target);

            if (failed)
            {
                _fileSystem.File.Move(_fileSystem.Path.Join(source, file), _fileSystem.Path.Join(target, AppendTimeStamp(file)));
                return true;
            }
            
            _fileSystem.File.Move(_fileSystem.Path.Join(source, file), _fileSystem.Path.Join(target, file));
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to move file due to an error: {Message}", e.Message);
        }
        return false;
    }

    public bool DeleteFile(string source, string file)
    {
        try
        {
            _fileSystem.File.Delete(_fileSystem.Path.Join(source, file));
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to delete file due to an error: {Message}", e.Message);
        }
        return false;
    }

    /// <inheritdoc />
    public DateTime GetCreationTimeUtc(string path)
        => _fileSystem.File.GetCreationTimeUtc(path);

    private string AppendTimeStamp(string fileName)
        => string.Concat(
            _fileSystem.Path.GetFileNameWithoutExtension(fileName),
            "_",
            _applicationTime.UtcNow().ToString("yyyyMMddHHmm"),
            _fileSystem.Path.GetExtension(fileName));
}