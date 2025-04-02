using FakeItEasy;
using Iris.Public.Types.Models.Public._2._3._1;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus;
using NServiceBus.Testing;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.EventHandlers.Nsb;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Queries;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.EventHandlers.Nsb;

public class ImageReceiptHandlerTest
{
    private readonly IMediator _mediator;
    private readonly ITransactionSupplier _transactionSupplier;
    private readonly ImageReceiptHandler _handler;
    private readonly IMessageHandlerContext _messageHandlerContext;
    private readonly FakeApplicationTime _applicationTime = new();


    public ImageReceiptHandlerTest()
    {
        var _logger = A.Fake<ILogger<ImageReceiptHandler>>();
        _mediator = A.Fake<IMediator>();
        _transactionSupplier = A.Fake<ITransactionSupplier>();
        _messageHandlerContext = new TestableMessageHandlerContext();

        _handler = new ImageReceiptHandler(_logger, _mediator, _transactionSupplier, _applicationTime);
    }

    [Fact]
    public async Task ShouldRecordImageReceipt_WhenImageLocalIdIsFound()
    {
        var imgReceipt = new ImageReceipt() { Success = true };
        A.CallTo(() => _mediator.Send(A<GetExamImageByLocalId>._, A<CancellationToken>._)).Returns(new ExamImage());
        A.CallTo(() => _mediator.Send(A<GetExamByImageLocalId>._, A<CancellationToken>._)).Returns(new Exam() { EvaluationId = 1});

        await _handler.Handle(imgReceipt, _messageHandlerContext);

        A.CallTo(() => _mediator.Send(A<GetExamImageByLocalId>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateStatus>.That.Matches(c =>
        c.MessageDateTime == _applicationTime.UtcNow()), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ShouldNotRecordImageReceipt_WhenLocalImageRecordNotFound()
    {
        var imgReceipt = new ImageReceipt() { Success = true };
        ExamImage nullImage = null;
        A.CallTo(() => _mediator.Send(A<GetExamImageByLocalId>._, A<CancellationToken>._)).Returns(nullImage);

        await _handler.Handle(imgReceipt, _messageHandlerContext);

        A.CallTo(() => _mediator.Send(A<GetExamImageByLocalId>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ShouldNotProcess_WhenSuccessIsFalse()
    {
        var imgReceipt = new ImageReceipt();

        await _handler.Handle(imgReceipt, _messageHandlerContext);

        A.CallTo(() => _mediator.Send(A<GetExamImageByLocalId>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }
}