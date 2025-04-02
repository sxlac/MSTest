using Iris.Public.Types.Models.V2_3_1;

namespace IrisPoc.Models.Image;

public class UploadImageRequest
{
    public string MemberPlanId => OrderRequest.Patient!.LocalId!;

    public OrderRequest OrderRequest { get; }

    public ImageModel Image { get; }

    public UploadImageRequest(OrderRequest orderRequest, ImageModel image)
    {
        OrderRequest = orderRequest;
        Image = image;
    }
}
