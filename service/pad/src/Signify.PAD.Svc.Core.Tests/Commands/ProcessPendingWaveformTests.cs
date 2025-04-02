using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using Signify.PAD.Svc.Core.Commands;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Infrastructure;
using Signify.PAD.Svc.Core.Queries;
using Signify.PAD.Svc.Core.Tests.Fakes.Infrastructure;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Commands;

public class ProcessPendingWaveformTests
{
    private readonly IApplicationTime _applicationTime = new FakeApplicationTime();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IMediator _mediator = A.Fake<IMediator>();

    private readonly ProcessPendingWaveformHandler _subject;

    public ProcessPendingWaveformTests()
    {
        _subject = new ProcessPendingWaveformHandler(A.Dummy<ILogger<ProcessPendingWaveformHandler>>(),
            _applicationTime, _mapper, _mediator);
    }

    [Fact]
    public async Task Handle_WhenUnableToQueryDb_ReturnsFailed()
    {
        A.CallTo(() => _mediator.Send(A<GetWaveformDocumentByFilename>._, A<CancellationToken>._))
            .Throws<Exception>();

        var actual = await _subject.Handle(new ProcessPendingWaveform(), default);

        Assert.False(actual.IsSuccessful);
        Assert.Null(actual.ClientId);

        A.CallTo(() => _mediator.Send(A<GetWaveformDocumentByFilename>._, A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(_mediator)
            .MustHaveHappenedOnceExactly(); // The asserted call above, and no others

        A.CallTo(_mapper)
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Handle_WhenAlreadyProcessed_DoesNothingAndReturnsFailed()
    {
        // Arrange
        const int padId = 1;
        const int memberPlanId = 2;
        const int clientId = 3;
        const string filename = "filename";
        var dateOfExam = DateTime.UtcNow;

        A.CallTo(() => _mediator.Send(A<GetWaveformDocumentByFilename>._, A<CancellationToken>._))
            .Returns(new WaveformDocument
            {
                Filename = filename,
                MemberPlanId = memberPlanId,
                DateOfExam = dateOfExam
            });
        A.CallTo(() => _mediator.Send(A<GetPadByMemberPlanId>._, A<CancellationToken>._))
            .Returns(new Core.Data.Entities.PAD
            {
                PADId = padId,
                ClientId = clientId
            });

        // Act
        var result = await _subject.Handle(new ProcessPendingWaveform
        {
            Filename = filename
        }, default);

        // Assert
        Assert.True(!result.IsSuccessful);

        A.CallTo(() => _mediator.Send(A<GetWaveformDocumentByFilename>.That.Matches(g => g.Filename == filename),
                A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Fact]
    public async Task Handle_WhenCannotAssociateFileWithPadRecord_DoesNothingAndReturnsUnsuccessful()
    {
        // Arrange
        const int memberPlanId = 1;
        const string filename = "filename";
        var dateOfExam = DateTime.UtcNow;
        
        var document = new WaveformDocument
        {
            WaveformDocumentId = 1,
            WaveformDocumentVendorId = 1,
            MemberPlanId = memberPlanId,
            DateOfExam = dateOfExam,
            Filename = filename
        };

        A.CallTo(() => _mediator.Send(A<GetWaveformDocumentByFilename>._, A<CancellationToken>._))
            .Returns((WaveformDocument)null);
        A.CallTo(() => _mediator.Send(A<GetPadByMemberPlanId>._, A<CancellationToken>._))
            .Returns((Core.Data.Entities.PAD)null);
        A.CallTo(() => _mapper.Map<WaveformDocument>(A<ProcessPendingWaveform>._))
            .Returns(document);

        // Act
        var result = await _subject.Handle(new ProcessPendingWaveform
        {
            Filename = filename
        }, default);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Null(result.ClientId);

        A.CallTo(() => _mediator.Send(A<GetWaveformDocumentByFilename>.That.Matches(g => g.Filename == filename),
                A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<GetPadByMemberPlanId>.That.Matches(g =>
                g.MemberPlanId == memberPlanId && g.DateOfService == dateOfExam), A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(_mediator)
            .MustHaveHappenedTwiceExactly(); // The asserted call above, and no others 
        A.CallTo(_mapper)
            .MustHaveHappened();
    }

    [Fact]
    public async Task Handle_HappyPath_Test()
    {
        // Arrange
        const int padId = 1;
        const int memberPlanId = 2;
        const int clientId = 3;
        const int evaluationId = 4;
        const string filename = "filename";
        const string filePath = "path";
        var dateOfExam = DateTime.UtcNow;

        var document = new WaveformDocument
        {
            MemberPlanId = memberPlanId,
            DateOfExam = dateOfExam,
            Filename = filename
        };

        var pad = new Core.Data.Entities.PAD
        {
            PADId = padId,
            EvaluationId = evaluationId,
            ClientId = clientId
        };

        var upload = new UploadPendingWaveformResult
        {
            IsSuccess = true
        };

        A.CallTo(() => _mediator.Send(A<GetWaveformDocumentByFilename>._, A<CancellationToken>._))
            .Returns((WaveformDocument)null);

        A.CallTo(() => _mapper.Map<WaveformDocument>(A<ProcessPendingWaveform>._))
            .Returns(document);

        A.CallTo(() => _mediator.Send(A<GetPadByMemberPlanId>._, A<CancellationToken>._))
            .Returns(pad);

        A.CallTo(() => _mediator.Send(A<CreateWaveformDocument>._, A<CancellationToken>._))
            .Returns(document);
        
        A.CallTo(() => _mediator.Send(A<UploadWaveformDocument>._, A<CancellationToken>._))
            .Returns(upload);

        // Act
        var result = await _subject.Handle(new ProcessPendingWaveform
        {
            Filename = filename,
            FilePath = filePath
        }, default);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal(clientId, result.ClientId);

        A.CallTo(() => _mediator.Send(A<GetWaveformDocumentByFilename>.That.Matches(g => g.Filename == filename),
                A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<GetPadByMemberPlanId>.That.Matches(g =>
                g.MemberPlanId == memberPlanId && g.DateOfService == dateOfExam), A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<CreateWaveformDocument>.That.Matches(c =>
                    c.Document == document && document.CreatedDateTime == _applicationTime.UtcNow()),
                A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<CreatePadStatus>.That.Matches(c =>
                    c.PadId == pad.PADId && c.StatusCode == PADStatusCode.WaveformDocumentDownloaded),
                A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreatePadStatus>.That.Matches(c =>
                    c.PadId == pad.PADId && c.StatusCode == PADStatusCode.WaveformDocumentUploaded),
                A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<UploadWaveformDocument>.That.Matches(u =>
                    u.EvaluationId == evaluationId && u.Filename == filename && u.FilePath == filePath),
                A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Fact]
    public async Task Handle_WhenUploadingDocumentFails_DoesNotSaveDocumentUploadedAndReturnsUnsuccessful()
    {
        // Arrange
        var pad = new Core.Data.Entities.PAD
        {
            PADId = 1,
            ClientId = 2 // To ensure it's not returned in the response despite being unsuccessful
        };

        var document = new WaveformDocument
        {
            MemberPlanId = 1 // To ensure it won't be viewed as a test PDF
        };

        A.CallTo(() => _mediator.Send(A<GetWaveformDocumentByFilename>._, A<CancellationToken>._))
            .Returns((WaveformDocument)null);

        A.CallTo(() => _mapper.Map<WaveformDocument>(A<ProcessPendingWaveform>._))
            .Returns(document);

        A.CallTo(() => _mediator.Send(A<GetPadByMemberPlanId>._, A<CancellationToken>._))
            .Returns(pad);

        A.CallTo(() => _mediator.Send(A<CreateWaveformDocument>._, A<CancellationToken>._))
            .Returns(new WaveformDocument());

        A.CallTo(() => _mediator.Send(A<UploadWaveformDocument>._, A<CancellationToken>._))
            .Throws<Exception>();

        // Act
        var result = await _subject.Handle(new ProcessPendingWaveform(), default);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Null(result.ClientId);

        // Although there's no easy way to ensure a transaction was created and then rolled back,
        // we can at least ensure WaveformDocumentDownloaded happened but not WaveformDocumentUploaded
        A.CallTo(() => _mediator.Send(A<CreatePadStatus>.That.Matches(c =>
                    c.StatusCode == PADStatusCode.WaveformDocumentDownloaded),
                A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreatePadStatus>.That.Matches(c =>
                    c.StatusCode == PADStatusCode.WaveformDocumentUploaded),
                A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Handle_WhenTestPdfGetsProcessed_DoesNotSaveDocumentAndMovesToIgnoreFile()
    {
        // Arrange
        // memberPlanId set to -2 identifies the pdf file as a Test PDF
        const int memberPlanId = -2;
        const string filename = "filename";
        const string filePath = "path";
        var dateOfExam = DateTime.UtcNow;

        var document = new WaveformDocument
        {
            MemberPlanId = memberPlanId,
            DateOfExam = dateOfExam,
            Filename = filename
        };

        A.CallTo(() => _mediator.Send(A<GetWaveformDocumentByFilename>._, A<CancellationToken>._))
            .Returns((WaveformDocument)null);

        A.CallTo(() => _mapper.Map<WaveformDocument>(A<ProcessPendingWaveform>._))
            .Returns(document);

        // Act
        var result = await _subject.Handle(new ProcessPendingWaveform
        {
            Filename = filename,
            FilePath = filePath
        }, default);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.True(result.IgnoreFile);

        A.CallTo(() => _mediator.Send(A<GetWaveformDocumentByFilename>.That.Matches(g => g.Filename == filename),
                A<CancellationToken>._))
            .MustHaveHappened();
    }
}