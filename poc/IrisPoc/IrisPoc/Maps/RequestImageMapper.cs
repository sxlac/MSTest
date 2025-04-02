using AutoMapper;
using Iris.Public.Types.Enums;
using Iris.Public.Types.Models;
using IrisPoc.Models.Image;
using IrisPoc.Models.Storage;

namespace IrisPoc.Maps;

/// <summary>
/// Maps this POC's internal models to the IRIS request model <see cref="RequestImage"/>
/// </summary>
public class RequestImageMapper :
    ITypeConverter<ImageModel, RequestImage>,
    ITypeConverter<UploadBlobResponse, RequestImage>
{
    public RequestImage Convert(ImageModel source, RequestImage destination, ResolutionContext context)
    {
        destination ??= new RequestImage();

        // There is a "LocalId" property that can correspond to some identifier defined by Signify/DEE

        destination.ImageContext = ImageContext.Primary; // Required

        destination.Laterality = GetLaterality(source); // Not required, but DEE supplies this today, and IRIS sometimes corrects it, if incorrect

        return destination;
    }

    public RequestImage Convert(UploadBlobResponse source, RequestImage destination, ResolutionContext context)
    {
        destination ??= new RequestImage();
        destination.AzureBlobStorage ??= new RequestAzureBlobStorage();

        destination.AzureBlobStorage.Container = source.ContainerName;
        destination.AzureBlobStorage.FileName = source.BlobName;

        return destination;
    }

    private static Laterality? GetLaterality(ImageModel image)
    {
        if ("left".Equals(image.Side, StringComparison.OrdinalIgnoreCase))
            return Laterality.OS;

        if ("right".Equals(image.Side, StringComparison.OrdinalIgnoreCase))
            return Laterality.OD;

        return null;
    }
}
