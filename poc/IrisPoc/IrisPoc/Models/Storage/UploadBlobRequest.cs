namespace IrisPoc.Models.Storage;

public class UploadBlobRequest
{
    /// <summary>
    /// Name to give the blob
    /// </summary>
    public string BlobName { get; }

    /// <summary>
    /// Blob contents
    /// </summary>
    public ReadOnlyMemory<byte> Contents { get; }

    public UploadBlobRequest(string blobName, byte[] contents)
    {
        BlobName = blobName;
        Contents = new ReadOnlyMemory<byte>(contents);
    }
}
