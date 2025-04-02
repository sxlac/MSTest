using Azure.Storage.Blobs;
using IrisPoc.Models.Storage;

namespace IrisPoc.Services.Storage;

public class AzureBlobStorageService : IBlobStorageService
{
    private readonly ILogger _logger;
    private readonly BlobContainerClient _client;

    public AzureBlobStorageService(ILogger<AzureBlobStorageService> logger, string uri)
    {
        _logger = logger;

        var containerUri = new Uri(uri);

        _client = new BlobContainerClient(containerUri, new BlobClientOptions
        {
            // Optional, but thought I'd play around with it
            Diagnostics =
            {
                IsLoggingEnabled = true,
                IsLoggingContentEnabled = true
            }
        });

        // Would be good to add something here that validates the client can connect, instead of having to wait
        // for the first upload request to occur -- fail fast on startup. Unfortunately, without attempting to
        // create a blob here, I'm not seeing any other way to verify this, because it doesn't look like we have
        // any read access to anything in the container. Possibly something we can ask IRIS?
    }

    public async Task<UploadBlobResponse> UploadBlob(UploadBlobRequest request, CancellationToken cancellationToken)
    {
        await _client.UploadBlobAsync(request.BlobName, new BinaryData(request.Contents), cancellationToken);

        _logger.LogInformation("Blob created in container {ContainerName} - {BlobName}",
            _client.Name, request.BlobName);

        return new UploadBlobResponse(_client.Name, request.BlobName);
    }
}
