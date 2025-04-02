using System;
using System.Collections.Generic;

namespace Signify.PAD.Svc.Core.Services.Interfaces;

public interface IDirectoryServices
{
    string GetIncomingDirectory(string vendorDirectory);
    string GetPendingDirectory(string vendorDirectory);
    string GetIgnoredDirectory(string vendorDirectory);
    string GetFileUploadedAlreadyDirectory(string vendorDirectory);
    string GetFileOlderThanThresholdDirectory(string vendorDirectory);
    string GetFilePendingAlreadyDirectory(string vendorDirectory);
    /// <summary>
    /// Directory format: \Processed\[VendorDirectory]\ClientId\[ClientId]\[yyyy]\[MM]\
    /// </summary>
    /// <param name="vendorDirectory"></param>
    /// <param name="clientId"></param>
    /// <returns></returns>
    string GetProcessedDirectory(string vendorDirectory, int? clientId);

    /// <summary>
    /// Gets the directory to move a Waveform pdf after successful processing
    /// </summary>
    /// <param name="vendorDirectory">The vendor-specific "processed" directory</param>
    /// <param name="clientId">The Client ID of the corresponding evaluation</param>
    /// <param name="processDateTime">Timestamp when the document was successfully processed</param>
    /// <returns></returns>
    string GetProcessedDirectory(string vendorDirectory, int? clientId, DateTimeOffset processDateTime);

    IEnumerable<string> GetFilesFromDirectory(string directory);
    bool MoveFileDirectory(string source, string target, string file, bool failed = false);
    bool DeleteFile(string source, string file);

    /// <summary>
    /// Returns the creation date and time of the specified file or directory
    /// </summary>
    /// <exception cref="ArgumentNullException" />
    /// <exception cref="UnauthorizedAccessException" />
    DateTime GetCreationTimeUtc(string path);
}