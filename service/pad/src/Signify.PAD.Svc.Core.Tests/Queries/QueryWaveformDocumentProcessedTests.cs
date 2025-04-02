using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Queries;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Queries;

public class QueryWaveformDocumentProcessedTests
{
    private readonly IMediator _mediator = A.Fake<IMediator>();

    private readonly QueryWaveformDocumentProcessedHandler _subject;

    public QueryWaveformDocumentProcessedTests()
    {
        _subject = new QueryWaveformDocumentProcessedHandler(A.Dummy<ILogger<QueryWaveformDocumentProcessedHandler>>(), _mediator);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_HappyPath(bool wasUploaded)
    {
        // Arrange
        A.CallTo(() => _mediator.Send(A<GetWaveformDocumentByFilename>._, A<CancellationToken>._))
            .Returns(new WaveformDocument());

        A.CallTo(() => _mediator.Send(A<GetPadByMemberPlanId>._, A<CancellationToken>._))
            .Returns(new Core.Data.Entities.PAD());

        A.CallTo(() => _mediator.Send(A<QueryPadStatusCode>._, A<CancellationToken>._))
            .Returns(wasUploaded);

        // Act
        var actual = await _subject.Handle(new QueryWaveformDocumentProcessed("filename"), default);

        // Assert
        Assert.Equal(wasUploaded, actual);
    }

    [Fact]
    public async Task Handle_WaveformDocument_NotFound()
    {
        // Arrange
        A.CallTo(() => _mediator.Send(A<GetWaveformDocumentByFilename>._, A<CancellationToken>._))
            .Returns((WaveformDocument)null);

        A.CallTo(() => _mediator.Send(A<GetPadByMemberPlanId>._, A<CancellationToken>._))
            .Returns(new Core.Data.Entities.PAD());

        A.CallTo(() => _mediator.Send(A<QueryPadStatusCode>._, A<CancellationToken>._))
            .Returns(false);

        // Act
        var actual = await _subject.Handle(new QueryWaveformDocumentProcessed("filename"), default);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public async Task Handle_Pad_NotFound()
    {
        // Arrange
        A.CallTo(() => _mediator.Send(A<GetWaveformDocumentByFilename>._, A<CancellationToken>._))
            .Returns(new WaveformDocument());

        A.CallTo(() => _mediator.Send(A<GetPadByMemberPlanId>._, A<CancellationToken>._))
            .Returns((Core.Data.Entities.PAD)null);

        A.CallTo(() => _mediator.Send(A<QueryPadStatusCode>._, A<CancellationToken>._))
            .Returns(false);

        // Act
        var actual = await _subject.Handle(new QueryWaveformDocumentProcessed("filename"), default);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public async Task Handle_FailedToDetermineDocument()
    {
        // Arrange
        A.CallTo(() => _mediator.Send(A<GetWaveformDocumentByFilename>._, A<CancellationToken>._))
            .Returns(new WaveformDocument());

        A.CallTo(() => _mediator.Send(A<GetPadByMemberPlanId>._, A<CancellationToken>._))
            .Returns((Task<Core.Data.Entities.PAD>)null);

        A.CallTo(() => _mediator.Send(A<QueryPadStatusCode>._, A<CancellationToken>._))
            .Returns(false);

        // Act
        var actual = await _subject.Handle(new QueryWaveformDocumentProcessed("filename"), default);

        // Assert
        Assert.False(actual);
    }
}