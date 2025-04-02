namespace IrisPoc.Services.IO;

/// <summary>
/// Interface to abstract IO from other services
/// </summary>
public interface IFileService
{
    string GetFileName(string filePath);

    Task<byte[]> ReadAllBytes(string filePath, CancellationToken cancellationToken);

    FileStream Open(string path);
}
