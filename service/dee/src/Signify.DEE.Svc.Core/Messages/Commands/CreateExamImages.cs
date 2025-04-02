using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewRelic.Api.Agent;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Messages.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Messages.Commands;

public class CreateExamImages : IRequest<Unit>
{
    public int ExamId { get; set; }
    public List<ExamImageModel> Images { get; set; }
}

public class CreateExamImagesHandler(ILogger<CreateExamImagesHandler> log, IMapper mapper, DataContext context)
    : IRequestHandler<CreateExamImages, Unit>
{
    [Trace]
    public async Task<Unit> Handle(CreateExamImages request, CancellationToken cancellationToken)
    {
        if (request.Images.Count == 0)
        {
            log.LogInformation("ExamId: {ExamId} -- No images uploaded to IRIS", request.ExamId);
            return Unit.Value;
        }

        var exam = context.Exams.Include(x => x.ExamImages).FirstOrDefault(ex => ex.ExamId == request.ExamId);

        if (exam == null)
        {
            throw new ArgumentNullException($"ExamId: {request.ExamId} -- No Exam record found");
        }

        log.LogDebug("ExamId: {ExamId} Image Count: {Count} -- IRIS returned images", request.ExamId, request.Images.Count);

        //Save Original images to database.
        foreach (var imgModel in request.Images.Where(i => i.ImageType == Constants.ApplicationConstants.OriginalImageType))
        {
            var img = mapper.Map<ExamImage>(imgModel);
            img.ImageLocalId = Guid.NewGuid().ToString();
            exam.ExamImages.Add(img);
            context.Entry(img).State = EntityState.Added;
        }

        await context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}