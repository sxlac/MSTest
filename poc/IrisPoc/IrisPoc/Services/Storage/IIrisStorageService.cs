using IrisPoc.Models.Image;

namespace IrisPoc.Services.Storage;

public interface IIrisStorageService
{
    public Task SubmitFile(UploadImageRequest request);
}
