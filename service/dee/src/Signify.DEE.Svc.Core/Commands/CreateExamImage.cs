using MediatR;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Messages.Models;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Commands;

[ExcludeFromCodeCoverage]
public class CreateExamImage : IRequest<ExamImage>
{
    public ExamModel Exam { get; set; }
}

public class CreateExamImageHandler(ILogger<CreateExamImageHandler> log, DataContext dataContext)
    : IRequestHandler<CreateExamImage, ExamImage>
{
    public async Task<ExamImage> Handle(CreateExamImage createExamImage, CancellationToken cancellationToken)
    {
        log.LogInformation("Saving image to database for exam local ID: {examLocalId}", createExamImage.Exam.ExamLocalId);

        var image = new ExamImage
        {
            ExamId = createExamImage.Exam.ExamId,
            ImageLocalId = Guid.NewGuid().ToString(),
        };

        dataContext.ExamImages.Add(image);
        await dataContext.SaveChangesAsync(cancellationToken);
        log.LogInformation("Saved image to database for exam local ID: {examLocalId}", createExamImage.Exam.ExamLocalId);

        return image;
    }
}