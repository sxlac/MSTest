using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Messages.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Messages.Commands;

public class UpdateExamImages : IRequest<bool>
{
    public List<ExamImageModel> Images { get; set; }
}

public class UpdateExamImagesHandler(IMapper mapper, DataContext context, ILogger<UpdateExamImagesHandler> logger)
    : IRequestHandler<UpdateExamImages, bool>
{
    [Trace]
    public async Task<bool> Handle(UpdateExamImages request, CancellationToken cancellationToken)
    {
        foreach (var imgModel in request.Images)
        {
            var img = mapper.Map<ExamImage>(imgModel);

            var image = context.ExamImages.FirstOrDefault(i => i.ImageLocalId == img.ImageLocalId);

            if (image != default)
            {
                if (img.LateralityCode == LateralityCode.Unknown)
                {
                    logger.LogWarning("Unable to resolve laterity for an image : {ExamImageId} belonging to Exam : {ExamId}", image.ExamImageId, image.ExamId);
                }
                image.LateralityCodeId = img.LateralityCode.LateralityCodeId;
#pragma warning disable CS0618 // Type or member is obsolete
                image.Gradable = img.Gradable;
                image.NotGradableReasons = img.NotGradableReasons;
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}