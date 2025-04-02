using AutoMapper;
using Iris.Public.Types.Models;
using IrisPoc.Models.Image;
using IrisPoc.Models.Storage;
using IrisPoc.Services.IO;
using IrisPoc.Services.Storage;
using IrisPoc.Settings;
using Microsoft.Extensions.Options;

namespace IrisPoc.Services.Image;

/// <inheritdoc />
public class ImageService : IImageService
{
    private readonly ImageSubmissionMode _submissionMode;
    private readonly IMapper _mapper;
    private readonly IFileService _fileService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IIrisStorageService _irisStorageService;

    public ImageService(IOptions<StartupSettings> options,
        IMapper mapper,
        IFileService fileService,
        IBlobStorageService blobStorageService,
        IIrisStorageService irisStorageService)
    {
        _submissionMode = options.Value.ImageSubmissionMode;
        _mapper = mapper;
        _fileService = fileService;
        _blobStorageService = blobStorageService;
        _irisStorageService = irisStorageService;
    }

    public Task UploadImage(UploadImageRequest request, CancellationToken cancellationToken)
    {
        switch (_submissionMode)
        {
            case ImageSubmissionMode.OnOrderCreation:
                return UploadToBlobStorage(request, cancellationToken);
            case ImageSubmissionMode.AfterOrderCreation:
                return UploadToIris(request);
            default:
                throw new NotImplementedException();
        }
    }

    private Task UploadToIris(UploadImageRequest request)
    {
        return _irisStorageService.SubmitFile(request);
    }

    private async Task UploadToBlobStorage(UploadImageRequest request, CancellationToken cancellationToken)
    {
        var bytes = await _fileService.ReadAllBytes(request.Image.FilePath!, cancellationToken);

        var uploadRequest = CreateUploadBlobRequest(request, bytes);

        var response = await _blobStorageService.UploadBlob(uploadRequest, cancellationToken);

        request.OrderRequest.Camera ??= new RequestCamera();
        request.OrderRequest.Camera.Images ??= new List<RequestImage>();

        var requestImage = _mapper.Map<RequestImage>(request.Image);
        _mapper.Map(response, requestImage);

        request.OrderRequest.Camera.Images.Add(requestImage);
    }

    private UploadBlobRequest CreateUploadBlobRequest(UploadImageRequest request, byte[] bytes)
    {
        var fileName = _fileService.GetFileName(request.Image.FilePath!);

        // This is just some way I'm uniquely creating blob names; we can discuss how best we wish to name them
        var blobName = $"{request.MemberPlanId}_{DateTime.UtcNow:yyyy.MM.dd_HHmmsss}_{fileName}";

        return new UploadBlobRequest(blobName, bytes);
    }
}
