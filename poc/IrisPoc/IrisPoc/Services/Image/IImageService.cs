using IrisPoc.Models.Image;

namespace IrisPoc.Services.Image;

/// <summary>
/// Intermediary image service, which abstracts where images are uploaded (blob storage in this case) and
/// blob naming convention
/// </summary>
public interface IImageService
{
    Task UploadImage(UploadImageRequest request, CancellationToken cancellationToken);
}
