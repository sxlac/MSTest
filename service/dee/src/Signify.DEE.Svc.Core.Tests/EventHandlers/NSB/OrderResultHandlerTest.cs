using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using Iris.Public.Types.Models;
using Iris.Public.Types.Models.V2_3_1;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.DEE.Core.Messages.Queries;
using Signify.DEE.Svc.Core.Commands;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.EventHandlers.Nsb;
using Signify.DEE.Svc.Core.Exceptions;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Models;
using Signify.DEE.Svc.Core.Messages.Queries;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.EventHandlers.Nsb;

public class OrderResultHandlerTest
{
    private readonly OrderResultHandler _orderResultHandler;
    private readonly TestableMessageHandlerContext _messageHandlerContext;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ITransactionSupplier _transactionSupplier;
    private readonly FakeApplicationTime _applicationTime = new();

    public OrderResultHandlerTest()
    {
        var logger = A.Fake<ILogger<OrderResultHandler>>();
        _mediator = A.Fake<IMediator>();
        _transactionSupplier = A.Fake<ITransactionSupplier>();
        _mapper = A.Fake<IMapper>();

        _orderResultHandler = new OrderResultHandler(logger, _mediator, _mapper, _transactionSupplier);

        _messageHandlerContext = new TestableMessageHandlerContext();
    }

    [Fact]
    public async Task Should_call_nsb_child_handlers()
    {
        var localId = Guid.NewGuid().ToString();
        // Arrange
        var orderResults = new OrderResult()
        {
            Order = new ResultOrder()
            {
                LocalId = localId
            },
            ResultsDocument = new ResultsDocument()
            {
                Content = "Some base64 string"
            },
            Gradings = new ResultGrading(),
            ImageDetails = new ResultImageDetails
            {
                LeftEyeOriginalCount = 0,
                RightEyeOriginalCount = 1
            }
        };

        var examModel = new ExamModel
        { DateOfService = _applicationTime.UtcNow(), MemberPlanId = 12, EvaluationId = 15, ExamLocalId = localId };
        var exam = new Exam() { EvaluationId = 15, ExamLocalId = localId };
        A.CallTo(() => _mediator.Send(A<GetExamByLocalId>._, A<CancellationToken>._)).Returns(exam);
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).Returns(new CreateStatusResponse(null, true));
        A.CallTo(() => _mediator.Send(A<ProcessIrisOrderResult>._, A<CancellationToken>._)).Returns(Unit.Value);
        A.CallTo(() => _mediator.Send(A<ProcessIrisImagesResult>._, A<CancellationToken>._)).Returns(Unit.Value);
        A.CallTo(() => _mediator.Send(A<GetHold>._, A<CancellationToken>._)).Returns(new Hold() { HoldId = 5, EvaluationId = 15 });
        A.CallTo(() => _mapper.Map<ExamModel>(exam)).Returns(examModel);

        var transaction = A.Fake<IBufferedTransaction>();
        A.CallTo(() => _transactionSupplier.BeginTransaction()).Returns(transaction);

        // Act
        await _orderResultHandler.Handle(orderResults, _messageHandlerContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<GetExamByLocalId>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();

        A.CallTo(() => _mediator.Send(A<ProcessIrisOrderResult>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();

        A.CallTo(() => _mediator.Send(A<ProcessIrisImagesResult>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();

        _messageHandlerContext.SentMessages.Length.Should().Be(4);

        var messagePublishResult = (PublishIrisOrderResult)_messageHandlerContext.SentMessages[0].Message;
        Assert.IsType<PublishIrisOrderResult>(messagePublishResult);
        Assert.Equal(messagePublishResult.Exam.EvaluationId, examModel.EvaluationId);

        var messageProcessPdf = (ProcessIrisResultPdf)_messageHandlerContext.SentMessages[1].Message;
        Assert.IsType<ProcessIrisResultPdf>(messageProcessPdf);
        Assert.Equal(messageProcessPdf.EvaluationId, examModel.EvaluationId);

        var addBillableStatus = (DetermineBillabityOfResult)_messageHandlerContext.SentMessages[2].Message;
        Assert.IsType<DetermineBillabityOfResult>(addBillableStatus);
        Assert.Equal(addBillableStatus.Exam.EvaluationId, examModel.EvaluationId);

        var reloadHoldCall = (ReleaseHold)_messageHandlerContext.SentMessages[3].Message;
        Assert.IsType<ReleaseHold>(reloadHoldCall);
        Assert.Equal(reloadHoldCall.Hold.EvaluationId, examModel.EvaluationId);

        A.CallTo(() => _transactionSupplier.BeginTransaction()).MustHaveHappened();
        A.CallTo(() => transaction.CommitAsync(A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => transaction.Dispose()).MustHaveHappened();
    }

    [Fact]
    public async Task UnmatchedOrder_ThrowsAnException()
    {
        // Arrange
        var orderResults = new OrderResult()
        {
            Order = new ResultOrder()
            {
                LocalId = "idThatWon'tBeMatched"
            }
        };
        Exam exam = null;
        A.CallTo(() => _mediator.Send(A<GetExamByLocalId>._, A<CancellationToken>._)).Returns(exam);
        _ = await Assert.ThrowsAsync<UnmatchedOrderException>(() => _orderResultHandler.Handle(orderResults, _messageHandlerContext));
    }
}