using AutoMapper;
using Iris.Public.Order;
using Iris.Public.Types.Models.V2_3_1;
using IrisPoc.Models;
using IrisPoc.Models.Image;
using IrisPoc.Services.Image;
using IrisPoc.Services.Storage;
using IrisPoc.Settings;
using Microsoft.Extensions.Options;

namespace IrisPoc.Services.Orders;

/// <summary>
/// Wrapper for IRIS's <see cref="OrderSubmissionService"/>, so that we can depend on
/// an interface instead of the concrete implementation
/// </summary>
public class OrderSubmissionServiceWrapper : IOrderSubmissionService
{
    private readonly ILogger _logger;
    private readonly ImageSubmissionMode _submissionMode;
    private readonly IMapper _mapper;
    private readonly OrderSubmissionService _orderSubmissionService;
    private readonly IImageService _imageService;

    public OrderSubmissionServiceWrapper(ILogger<OrderSubmissionServiceWrapper> logger,
        string serviceBusConnectionString,
        IOptions<StartupSettings> options,
        IMapper mapper,
        IImageService imageService)
    {
        _logger = logger;
        _submissionMode = options.Value.ImageSubmissionMode;
        _mapper = mapper;
        _orderSubmissionService = new OrderSubmissionService(serviceBusConnectionString);
        _imageService = imageService;
    }

    /// <inheritdoc />
    public async Task SubmitRequest(ExamModel exam, CancellationToken cancellationToken)
    {
        var orderRequest = _mapper.Map<OrderRequest>(exam);

        switch (_submissionMode)
        {
            case ImageSubmissionMode.OnOrderCreation:
                // Submit images first directly to Azure Storage Blob, then add reference to them in the order creation request
                await UploadImages(exam, orderRequest, cancellationToken);
                await _orderSubmissionService.SubmitRequest(orderRequest);
                break;
            case ImageSubmissionMode.AfterOrderCreation:
                // Upload the images to IRIS after creating the order, using IRIS's ImageSubmissionService
                await _orderSubmissionService.SubmitRequest(orderRequest);
                await UploadImages(exam, orderRequest, cancellationToken);
                break;
        }

        _logger.LogInformation("Submitting order for {MemberPlanId}", orderRequest.Patient!.LocalId);
    }

    private async Task UploadImages(ExamModel model, OrderRequest orderRequest, CancellationToken cancellationToken)
    {
        foreach (var image in model.Images)
        {
            var request = new UploadImageRequest(orderRequest, image);

            await _imageService.UploadImage(request, cancellationToken);
        }
    }
}
