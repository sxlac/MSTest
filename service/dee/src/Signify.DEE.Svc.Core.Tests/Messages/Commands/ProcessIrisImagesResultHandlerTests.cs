using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Exceptions;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Queries;
using Signify.DEE.Svc.Core.Tests.Mocks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Commands;

public class ProcessIrisImagesResultHandlerTests
{
    private readonly ILogger<ProcessIrisImagesResultHandler> _logger;
    private readonly IMediator _mediator;
    private readonly ProcessIrisImagesResultHandler _handler;

    public ProcessIrisImagesResultHandlerTests()
    {
        _logger = A.Fake<ILogger<ProcessIrisImagesResultHandler>>();
        _mediator = A.Fake<IMediator>();
        _handler = new ProcessIrisImagesResultHandler(_logger, _mediator);

    }

    [Fact]
    public async Task ProcessIrisImagesResultHandler_HandleOrderResult_SendUpdateExamImagesMediatrRequest_AddExamLaterality_Without_NonGradableReason()
    {
        // Arrange
        var processIrisImagesResult = new ProcessIrisImagesResult
        {
            OrderResult = OrderResultMock.BuildOrderResult(),
            Exam = ExamModelMock.BuildExamModel()
        };

        var images = new List<ExamImage>() { new ExamImage() { ImageLocalId = "1" }, new ExamImage() { ImageLocalId = "2" } };
        A.CallTo(() => _mediator.Send(A<GetExamImages>._, A<CancellationToken>._)).Returns(images);

        // Act
        await _handler.Handle(processIrisImagesResult, CancellationToken.None);

        // Assert
        A.CallTo(() => _mediator.Send(A<GetExamImages>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateExamImageModelRecords>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateLateralityGrade>._, A<CancellationToken>._)).MustHaveHappenedTwiceExactly();
        A.CallTo(() => _mediator.Send(A<CreateNonGradableReasons>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<UpdateExamImages>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ProcessIrisImagesResultHandler_HandleOrderResult_SendUpdateExamImagesMediatrRequest_AddExamLaterality_With_NonGradableReason()
    {
        // Arrange
        var orderResult = OrderResultMock.BuildOrderResult();
        orderResult.Gradings.OD.Gradable = false;
        orderResult.Gradings.OD.UngradableReasons = new List<string>() { "Image blurry", "Image Media Opacity" };

        orderResult.Gradings.OS.Gradable = false;
        orderResult.Gradings.OS.UngradableReasons = new List<string>() { "Image blurry", "Image Media Opacity" };

        var processIrisImagesResult = new ProcessIrisImagesResult
        {
            OrderResult = orderResult,
            Exam = ExamModelMock.BuildExamModel()
        };

        var images = new List<ExamImage>() { new ExamImage() { ImageLocalId = "1" }, new ExamImage() { ImageLocalId = "2" } };
        A.CallTo(() => _mediator.Send(A<GetExamImages>._, A<CancellationToken>._)).Returns(images);

        // Act
        await _handler.Handle(processIrisImagesResult, CancellationToken.None);

        // Assert
        A.CallTo(() => _mediator.Send(A<GetExamImages>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateExamImageModelRecords>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateLateralityGrade>._, A<CancellationToken>._)).MustHaveHappenedTwiceExactly();
        A.CallTo(() => _mediator.Send(A<CreateNonGradableReasons>._, A<CancellationToken>._)).MustHaveHappenedTwiceExactly();
        A.CallTo(() => _mediator.Send(A<UpdateExamImages>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task WhenSingleEyeOrder_AppendEnucleationToNotGradeableReasons()
    {
        // Arrange
        var orderResult = OrderResultMock.BuildOrderResultWithEnucleation();

        var processIrisImagesResult = new ProcessIrisImagesResult
        {
            OrderResult = orderResult,
            Exam = ExamModelMock.BuildExamModel()
        };

        var images = new List<ExamImage>() { new ExamImage() { ImageLocalId = "2" } };
        A.CallTo(() => _mediator.Send(A<GetExamImages>._, A<CancellationToken>._)).Returns(images);

        // Act
        await _handler.Handle(processIrisImagesResult, CancellationToken.None);

        // Assert
        Assert.Equal(2, orderResult.Gradings.OD.UngradableReasons.ToList().Count);
        Assert.Equal("Enucleation", orderResult.Gradings.OD.UngradableReasons.ToList()[1]);
    }

    [Fact]
    public async Task WhenVendorImageHasUnmatchedLocalId_Throw()
    {
        // Arrange
        var orderResult = OrderResultMock.BuildOrderResult();

        var processIrisImagesResult = new ProcessIrisImagesResult
        {
            OrderResult = orderResult,
            Exam = ExamModelMock.BuildExamModel()
        };

        var examImagesWithUnmatchedLocalId = new List<ExamImage>() { new ExamImage() { ImageLocalId = "0" }, new ExamImage() { ImageLocalId = "2" } };
        A.CallTo(() => _mediator.Send(A<GetExamImages>._, A<CancellationToken>._)).Returns(examImagesWithUnmatchedLocalId);

        // Act
        _ = await Assert.ThrowsAsync<UnmatchedVendorImageException>(() => _handler.Handle(processIrisImagesResult, CancellationToken.None));
    }
}