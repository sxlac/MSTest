using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.DEE.Svc.Core.Commands;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Exceptions;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Queries;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Commands;

public class ProcessIrisResultPdfTests
{
    private readonly ProcessIrisResultPdfHandler _processIrisResultPdfHandler;
    private readonly TestableMessageHandlerContext _messageHandlerContext;
    private readonly IMediator _mediator;
    private readonly FakeApplicationTime _applicationTime = new();

    public ProcessIrisResultPdfTests()
    {
        var logger = A.Fake<ILogger<ProcessIrisResultPdfHandler>>();
        _mediator = A.Fake<IMediator>();
        var transactionSupplier = A.Fake<ITransactionSupplier>();
        _processIrisResultPdfHandler = new ProcessIrisResultPdfHandler(logger, _mediator, transactionSupplier, _applicationTime);
        _messageHandlerContext = new TestableMessageHandlerContext();
    }

    [Fact]
    public async Task Should_Upload_Pdf_when_Pdf_valid()
    {
        //Arrange
        byte[] bytes = { 2, 4, 6, 8, 10, 12, 14, 16, 18, 20 };
        var s = Convert.ToBase64String(bytes);

        var message = new ProcessIrisResultPdf
        {
            EvaluationId = 1,
            CreatedDateTime = _applicationTime.UtcNow(),
            PdfData = s,
            ExamId = 3,
        };

        A.CallTo(() => _mediator.Send(A<GetPdfDataFromString>._, A<CancellationToken>._)).Returns(bytes);
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).Returns(new CreateStatusResponse(null, true));
        A.CallTo(() => _mediator.Send(A<CreateExamResultPdf>._, A<CancellationToken>._)).Returns(Unit.Value);

        //Act
        await _processIrisResultPdfHandler.Handle(message, _messageHandlerContext);

        //Assert
        A.CallTo(() => _mediator.Send(A<GetPdfDataFromString>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateExamResultPdf>._, A<CancellationToken>._)).MustHaveHappened();
    }

    [Fact]
    public async Task Should_Throw_Exception_Pdf_When_Pdf_Invalid()
    {
        //Arrange
        byte[] bytes = { 2, 4, 6, 8, 10, 12, 14, 16, 18, 20 };
        var s = Convert.ToBase64String(bytes);

        var message = new ProcessIrisResultPdf
        {
            EvaluationId = 1,
            CreatedDateTime = _applicationTime.UtcNow(),
            PdfData = s,
            ExamId = 3,
        };

        //Act
        A.CallTo(() => _mediator.Send(A<GetPdfDataFromString>._, A<CancellationToken>._)).Returns((byte[])null);
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).Returns(new CreateStatusResponse(null, true));
        A.CallTo(() => _mediator.Send(A<CreateExamResultPdf>._, A<CancellationToken>._)).Returns(Unit.Value);

        //Assert
        _ = await Assert.ThrowsAsync<InvalidPdfException>(() => _processIrisResultPdfHandler.Handle(message, _messageHandlerContext));
    }

    [Fact]
    public async Task Should_Throw_Exception_Pdf_When_Pdf_Not_In_Correct_Format()
    {
        //Arrange
        var message = new ProcessIrisResultPdf
        {
            EvaluationId = 1,
            CreatedDateTime = _applicationTime.UtcNow(),
            PdfData = string.Empty,
            ExamId = 3,
        };

        A.CallTo(() => _mediator.Send(A<GetPdfDataFromString>._, A<CancellationToken>._)).Returns((byte[])null);
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).Returns(new CreateStatusResponse(null, true));
        A.CallTo(() => _mediator.Send(A<CreateExamResultPdf>._, A<CancellationToken>._)).Returns(Unit.Value);

        //Act
        _ = await Assert.ThrowsAsync<InvalidPdfException>(() => _processIrisResultPdfHandler.Handle(message, _messageHandlerContext));

        //Assert
        A.CallTo(() => _mediator.Send(A<GetPdfDataFromString>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateExamResultPdf>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).MustNotHaveHappened();
    }
}