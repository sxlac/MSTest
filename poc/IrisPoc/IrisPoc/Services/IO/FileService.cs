namespace IrisPoc.Services.IO;

/// <inheritdoc />
public class FileService : IFileService
{
    public string GetFileName(string filePath)
        => Path.GetFileName(filePath);

    public Task<byte[]> ReadAllBytes(string filePath, CancellationToken cancellationToken)
        => File.ReadAllBytesAsync(filePath, cancellationToken);

    public FileStream Open(string path)
        => File.Open(path, FileMode.Open);
}
