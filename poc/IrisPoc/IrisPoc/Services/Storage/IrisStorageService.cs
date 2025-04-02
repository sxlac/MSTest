using Iris.Public.Image;
using Iris.Public.Types.Models.Public._2._3._1;
using IrisPoc.Models.Image;
using IrisPoc.Services.IO;

namespace IrisPoc.Services.Storage;

public class IrisStorageService : IIrisStorageService
{
    private readonly ILogger _logger;
    private readonly ImageSubmissionService _imageService;
    private readonly IFileService _fileService;

    public IrisStorageService(ILogger<IrisStorageService> logger, string connectionString, IFileService fileService)
    {
        _logger = logger;
        _imageService = new ImageSubmissionService(connectionString);
        _fileService = fileService;
    }

    public async Task SubmitFile(UploadImageRequest request)
    {
        await using var stream = _fileService.Open(request.Image.FilePath!);

        await _imageService.SubmitFileAsync(new ImageRequest(request.MemberPlanId), stream);

        _logger.LogInformation("Successfully uploaded image to IRIS for {MemberPlanId} - {FilePath}", request.MemberPlanId, request.Image.FilePath);
    }
}
