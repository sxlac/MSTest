using IrisPoc.Models.Storage;

namespace IrisPoc.Services.Storage;

/// <summary>
/// Interface for a service to interact with the blob storage service of our choosing
/// </summary>
public interface IBlobStorageService
{
    Task<UploadBlobResponse> UploadBlob(UploadBlobRequest request, CancellationToken cancellationToken);
}
