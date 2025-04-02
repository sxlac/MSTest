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

public class OrderReceiptHandlerTest
{
    private readonly IMediator _mediator;
    private readonly ITransactionSupplier _transactionSupplier;
    private readonly OrderReceiptHandler _handler;
    private readonly IMessageHandlerContext _messageHandlerContext;
    private readonly FakeApplicationTime _applicationTime = new();


    public OrderReceiptHandlerTest()
    {
        var _logger = A.Fake<ILogger<OrderReceiptHandler>>();
        _mediator = A.Fake<IMediator>();
        _transactionSupplier = A.Fake<ITransactionSupplier>();
        _messageHandlerContext = new TestableMessageHandlerContext();

        _handler = new OrderReceiptHandler(_logger, _mediator, _transactionSupplier, _applicationTime);
    }

    [Fact]
    public async Task ShouldRecordOrderReceipt_WhenExamLocalIdIsFound()
    {
        var orderReceipt = new OrderReceipt() { Success = true };
        A.CallTo(() => _mediator.Send(A<GetExamByLocalId>._, A<CancellationToken>._)).Returns(new Exam() { EvaluationId = 1});

        await _handler.Handle(orderReceipt, _messageHandlerContext);

        A.CallTo(() => _mediator.Send(A<GetExamByLocalId>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateStatus>.That.Matches(c =>
        c.MessageDateTime == _applicationTime.UtcNow()), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ShouldNotRecordOrderReceipt_WhenLocalExamRecordNotFound()
    {
        var orderReceipt = new OrderReceipt() { Success = true };
        Exam nullExam = null;
        A.CallTo(() => _mediator.Send(A<GetExamByLocalId>._, A<CancellationToken>._)).Returns(nullExam);

        await _handler.Handle(orderReceipt, _messageHandlerContext);

        A.CallTo(() => _mediator.Send(A<GetExamByLocalId>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ShouldNotProcess_WhenSuccessIsFalse()
    {
        var orderReceipt = new OrderReceipt();

        await _handler.Handle(orderReceipt, _messageHandlerContext);

        A.CallTo(() => _mediator.Send(A<GetExamByLocalId>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }
}