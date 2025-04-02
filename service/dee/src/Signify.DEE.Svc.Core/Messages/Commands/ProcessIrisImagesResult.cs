using Iris.Public.Types.Models;
using Iris.Public.Types.Models.V2_3_1;
using MediatR;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.Constants;
using Signify.DEE.Svc.Core.Exceptions;
using Signify.DEE.Svc.Core.Messages.Models;
using Signify.DEE.Svc.Core.Messages.Queries;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Messages.Commands;

public class ProcessIrisImagesResult : IRequest<Unit>
{
    public OrderResult OrderResult { get; set; }
    public ExamModel Exam { get; set; }
}

public class ProcessIrisImagesResultHandler(ILogger<ProcessIrisImagesResultHandler> logger, IMediator mediator)
    : IRequestHandler<ProcessIrisImagesResult, Unit>
{
    [Transaction]
    public async Task<Unit> Handle(ProcessIrisImagesResult request, CancellationToken cancellationToken)
    {
        logger.LogDebug("ExamId:{ExamId} -- handler ProcessIrisImagesResultHandler started", request.Exam.ExamId);

        // Retrieve Images we have stored for exam
        var examImages = await mediator.Send(new GetExamImages { ExamId = request.Exam.ExamId }, cancellationToken).ConfigureAwait(false);

        // Check that all of the images we received belong to this exam but ignore null local Id 
        // these are enhanced images which we don't process.
        foreach (var irisImg in request.OrderResult.Images.Where(i => i.LocalId is not null))
        {
            if (examImages.FirstOrDefault(i => irisImg.LocalId == i.ImageLocalId) is null)
            {
                logger.LogWarning("Unmatched vendor image local id {LocalId} for exam {ExamId}", irisImg.LocalId, request.Exam.ExamId);
                throw new UnmatchedVendorImageException(irisImg.LocalId, request.Exam.ExamId);
            }
        }

        // When Enucleation is present, we must add it to the list of not-gradeable reasons for reporting
        if (request.OrderResult.Order.SingleEyeOnly)
        {
            if (request.OrderResult.ImageDetails.RightEyeCount == 0)
            {
                var oDReasons = request.OrderResult.Gradings.OD.UngradableReasons != null ? request.OrderResult.Gradings.OD.UngradableReasons.ToList() : new List<string>();
                oDReasons.Add(ApplicationConstants.Enucleation);
                request.OrderResult.Gradings.OD.UngradableReasons = oDReasons;
            }
            else if (request.OrderResult.ImageDetails.LeftEyeCount == 0)
            {
                var oSReasons = request.OrderResult.Gradings.OS.UngradableReasons != null ? request.OrderResult.Gradings.OS.UngradableReasons.ToList() : new List<string>();
                oSReasons.Add(ApplicationConstants.Enucleation);
                request.OrderResult.Gradings.OS.UngradableReasons = oSReasons;
            }
        }

        // Map Grading Results to existing Images
        var examImageModel = await mediator.Send(new CreateExamImageModelRecords { DbImages = examImages, IrisImages = request.OrderResult.Images, Gradings = request.OrderResult.Gradings }, cancellationToken).ConfigureAwait(false);

        // MediatR call to update images Laterality
        await mediator.Send(new UpdateExamImages { Images = examImageModel.ToList() }, cancellationToken).ConfigureAwait(false);

        //Add Right eye grade and Non-gradable Reason (If available)
        if (request.OrderResult.Gradings?.OD != null)
        {
            await AddLateralityGrade(request.Exam, request.OrderResult.Gradings!.OD, ApplicationConstants.Laterality.RightEyeCode);
        }

        //Add Left eye grade and Non-gradable Reason (If available)
        if (request.OrderResult.Gradings?.OS != null)
        {
            await AddLateralityGrade(request.Exam, request.OrderResult.Gradings!.OS, ApplicationConstants.Laterality.LeftEyeCode);
        }
        return Unit.Value;
    }

    private async Task AddLateralityGrade(ExamModel examModel, ResultEyeSideGrading sideGrading, string LateralityCode)
    {
        int examLateralityGradeId = await mediator.Send(new CreateLateralityGrade()
        {
            ExamModel = examModel,
            Grading = sideGrading,
            LateralityCode = LateralityCode
        });

        if (!sideGrading.Gradable.HasValue || !sideGrading.Gradable.Value)
        {
            await mediator.Send(new CreateNonGradableReasons()
            {
                ExamLateralityGradeId = examLateralityGradeId,
                Reasons = sideGrading.UngradableReasons.ToList()
            });
        }
    }
}