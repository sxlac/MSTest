using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.DEE.Messages.Status;
using Signify.DEE.Svc.Core.Commands;
using Signify.DEE.Svc.Core.Data;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Models;
using Signify.DEE.Svc.Core.Messages.Queries;
using Signify.DEE.Svc.Core.Tests.Mocks;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Commands;

public class PublishIrisOrderResultTests
{
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly ITransactionSupplier _transactionSupplier;
    private readonly FakeApplicationTime _applicationTime = new();
    private readonly PublishIrisOrderResultHandler _handler;
    private readonly TestableMessageHandlerContext _messageHandlerContext;

    public PublishIrisOrderResultTests()
    {
        var logger = A.Fake<ILogger<PublishIrisOrderResultHandler>>();
        _mapper = A.Fake<IMapper>();
        _mediator = A.Fake<IMediator>();
        _transactionSupplier = A.Fake<ITransactionSupplier>();
        _handler = new PublishIrisOrderResultHandler(logger, _mapper, _mediator, _transactionSupplier, _applicationTime);
        _messageHandlerContext = new TestableMessageHandlerContext();
    }

    [Fact]
    public async Task PublishIrisOrderResultHandler_ProcessRequest_PublishMessagesSuccessfully()
    {
        // Arrange
        var exam = ExamModelMock.BuildExamModel();
        var publishIrisOrderResult = new PublishIrisOrderResult
        {
            Exam = exam
        };

        A.CallTo(() => _mediator.Send(A<CreateStatus>._, default)).Returns(new CreateStatusResponse(ExamStatusEntityMock.BuildExamStatus(exam.ExamId, ExamStatusCode.ExamCreated.ExamStatusCodeId), true));
        A.CallTo(() => _mapper.Map<ExamModel>(A<Exam>._)).Returns(ExamModelMock.BuildExamModel());
        A.CallTo(() => _mapper.Map<ResultsReceived>(A<ExamModel>._)).Returns(ResultsReceivedMock.BuildResultsReceived());
        A.CallTo(() => _mediator.Send(A<PublishStatusUpdate>._, default)).Returns(new Unit());
        A.CallTo(() => _mediator.Send(A<GetResultReceivedData>._, default)).Returns(ResultMock.BuildResultMock());
        A.CallTo(() => _mediator.Send(A<PublishResult>._, default)).Returns(new Unit());
        var transaction = A.Fake<IBufferedTransaction>();
        A.CallTo(() => _transactionSupplier.BeginTransaction()).Returns(transaction);

        // Act
        await _handler.Handle(publishIrisOrderResult, _messageHandlerContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<CreateStatus>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<GetResultReceivedData>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _mediator.Send(A<PublishResult>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _transactionSupplier.BeginTransaction()).MustHaveHappened();
        A.CallTo(() => transaction.CommitAsync(A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => transaction.Dispose()).MustHaveHappened();
        
    }
    
}