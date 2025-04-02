using AutoMapper;
using Iris.Public.Image;
using Iris.Public.Types.Models.Public._2._3._1;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Constants;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Signify.DEE.Svc.Core.Commands;

[ExcludeFromCodeCoverage]
public class UploadIrisImages : IRequest<bool>
{
    public ImageRequest Request { get; set; }
    public ExamModel Exam { get; set; }
    public ExamAnswersModel ExamAnswers { get; set; }
    public Dictionary<string, string> ImageIdToRawImageMap { get; set; }
}

public class UploadIrisImagesHandler(
    ILogger<UploadIrisImagesHandler> log,
    ImageSubmissionService imageSubmissionService,
    IMapper mapper,
    IMediator mediator)
    : IRequestHandler<UploadIrisImages, bool>
{
    public async Task<bool> Handle(UploadIrisImages uploadIrisImages, CancellationToken cancellationToken)
    {
        log.LogInformation("Preparing to upload images for Order Id: {examLocalId}", uploadIrisImages.Exam.ExamLocalId);

        uploadIrisImages.Request = mapper.Map<ImageRequest>(uploadIrisImages);

        foreach (var kvp in uploadIrisImages.ImageIdToRawImageMap)
        {
            uploadIrisImages.Request.Image.LocalId = kvp.Key;
            var image = Convert.FromBase64String(kvp.Value);
            using var stream = new MemoryStream(image);
            await imageSubmissionService.SubmitFileAsync(uploadIrisImages.Request, stream);
        }

        log.LogInformation("Images successfully uploaded to Iris for Order Id: {examLocalId}", uploadIrisImages.Exam.ExamLocalId);
        await mediator.Send(new RegisterObservabilityEvent { EvaluationId = uploadIrisImages.Exam.EvaluationId, EventType = Observability.DeeStatusEvents.ImagesSentToIrisEvent }, cancellationToken);

        return true;
    }
}