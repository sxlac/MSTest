namespace IrisPoc.Models.Storage;

public class UploadBlobResponse
{
    /// <summary>
    /// Container the blob was created inside
    /// </summary>
    public string ContainerName { get; }

    /// <summary>
    /// Name of the blob
    /// </summary>
    public string BlobName { get; }

    public UploadBlobResponse(string containerName, string blobName)
    {
        ContainerName = containerName;
        BlobName = blobName;
    }
}
