using AutoMapper;
using Iris.Public.Types.Enums;
using Iris.Public.Types.Models.Public._2._3._1;
using Signify.DEE.Svc.Core.Commands;
using Signify.DEE.Svc.Core.Configs;

namespace Signify.DEE.Svc.Core.Maps;

public class IrisImageUploadMapper(IrisConfig irisConfig) : ITypeConverter<UploadIrisImages, ImageRequest>
{
    public ImageRequest Convert(UploadIrisImages source, ImageRequest destination, ResolutionContext context)
    {
        destination ??= new ImageRequest();
        destination.OrderLocalId = source.Exam.ExamLocalId;
        destination.TotalImageCountForOrder = source.ImageIdToRawImageMap.Keys.Count;

        destination.Image = new Iris.Public.Types.Models.RequestImage()
        {
            Taken = source.Exam.DateOfService,
            ImageContext = ImageContext.Primary
        };

        destination.ItemNumberInCollection = source.ExamAnswers.Images.Count;
        destination.ImageEncoding = ImageEncoding.PNG;
        destination.ClientGuid = irisConfig.ClientGuid;
        destination.ImageClass = ImageClass.Fundus;

        destination.Site = new Iris.Public.Types.Models.RequestSite()
        {
            LocalId = irisConfig.SiteLocalId
        };

        return destination;
    }
}