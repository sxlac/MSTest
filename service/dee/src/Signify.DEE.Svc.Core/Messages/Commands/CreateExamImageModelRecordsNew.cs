using AutoMapper;
using Iris.Public.Types.Models;
using MediatR;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.Constants;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Messages.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Messages.Commands;

public class CreateExamImageModelRecords : IRequest<List<ExamImageModel>>
{
    public ResultGrading Gradings { get; set; }
    public IEnumerable<ResultImage> IrisImages { get; set; }
    public IEnumerable<ExamImage> DbImages { get; set; }
}

public class CreateExamImageModelRecordsHandler(IMapper mapper)
    : IRequestHandler<CreateExamImageModelRecords, List<ExamImageModel>>
{
    [Transaction]
    public Task<List<ExamImageModel>> Handle(CreateExamImageModelRecords request, CancellationToken cancellationToken)
    {
        var examImagesModel = new List<ExamImageModel>();

        foreach (var image in request.DbImages)
        {
            var examImageModel = mapper.Map<ExamImageModel>(image);

            var irisImage = request.IrisImages.FirstOrDefault(img => img.LocalId == image.ImageLocalId);

            if (irisImage != null)
            {
                switch (irisImage.Laterality)
                {
                    case Iris.Public.Types.Enums.Laterality.OS:
                        UpdateLaterality(ApplicationConstants.Laterality.LeftEyeCode);
                        break;
                    case Iris.Public.Types.Enums.Laterality.OD:
                        UpdateLaterality(ApplicationConstants.Laterality.RightEyeCode);
                        break;
                    case null:
                        break;
                    default:
                        // This way, if their library is updated to include a new enumeration that we're not aware of, a unit test will fail, notifying us of the addition
                        throw new NotImplementedException("Unhandled laterality from Iris: " + irisImage.Laterality);
                }

                void UpdateLaterality(string laterality)
                {
                    if (examImageModel.Laterality == laterality)
                        return;

                    examImageModel.Laterality = laterality;
                }
            }

            switch (examImageModel.Laterality)
            {
                case ApplicationConstants.Laterality.RightEyeCode:
                    AddSideResults(examImageModel, request.Gradings?.OD); break;
                case ApplicationConstants.Laterality.LeftEyeCode:
                    AddSideResults(examImageModel, request.Gradings?.OS); break;
            }

            examImagesModel.Add(examImageModel);
        }

        return Task.FromResult(examImagesModel);
    }

    private static void AddSideResults(ExamImageModel examImageModel, ResultEyeSideGrading sideGrading)
    {
        if (sideGrading is not { Gradable: not null })
            return;

#pragma warning disable CS0618 // Type or member is obsolete
        examImageModel.Gradable = sideGrading.Gradable.Value;
        if (!examImageModel.Gradable && sideGrading.UngradableReasons != null)
            examImageModel.NotGradableReasons = sideGrading.UngradableReasons.ToList();
#pragma warning restore CS0618 // Type or member is obsolete
    }
}