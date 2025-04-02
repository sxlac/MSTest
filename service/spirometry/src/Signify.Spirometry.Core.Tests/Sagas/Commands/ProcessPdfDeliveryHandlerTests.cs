using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Events;
using Signify.Spirometry.Core.Infrastructure;
using Signify.Spirometry.Core.Queries;
using SpiroNsb.SagaCommands;
using SpiroNsb.SagaEvents;
using SpiroNsbEvents;
using System.Threading.Tasks;
using System.Threading;
using Xunit;
using StatusCode = Signify.Spirometry.Core.Models.StatusCode;

namespace Signify.Spirometry.Core.Tests.Sagas.Commands;

public class ProcessPdfDeliveryHandlerTests
{
    private const long EvaluationId = 1;
    private const int PdfDeliveryId = 2;

    private readonly IApplicationTime _applicationTime = new FakeApplicationTime();
    private readonly ITransactionSupplier _transactionSupplier = A.Fake<ITransactionSupplier>();
    private readonly IBufferedTransaction _transaction = A.Fake<IBufferedTransaction>();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly IMediator _mediator = A.Fake<IMediator>();

    private readonly TestableMessageHandlerContext _context = new();

    public ProcessPdfDeliveryHandlerTests()
    {
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .Returns(_transaction);
    }

    private ProcessPdfDeliveryHandler CreateSubject()
        => new ProcessPdfDeliveryHandler(A.Dummy<ILogger<ProcessPdfDeliveryHandler>>(),
            _applicationTime, _transactionSupplier, _mapper, _mediator);

    /// <summary>
    /// Asserts that a PdfDeliveryProcessedEvent was raised to the context
    /// </summary>
    private void AssertMarkedAsProcessed()
    {
        var message = _context.FindSentMessage<PdfDeliveryProcessedEvent>();

        Assert.NotNull(message);
        Assert.Equal(_applicationTime.UtcNow(), message.CreatedDateTime);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WhetherBillableOrNot_SendsClientPdfDeliveredStatus(bool isBillable)
    {
        // Arrange
        var request = new ProcessPdfDelivery(EvaluationId, PdfDeliveryId, isBillable);

        // Act
        await CreateSubject().Handle(request, _context);

        // Assert
        A.CallTo(() => _mediator.Send(A<QueryPdfDeliveredToClient>.That.Matches(q =>
                    q.EvaluationId == EvaluationId),
                A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>.That.Matches(e =>
                    e.StatusCode == StatusCode.ClientPdfDelivered),
                A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _transaction.CommitAsync(A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _transaction.Dispose())
            .MustHaveHappened();

        AssertMarkedAsProcessed();
    }

    [Fact]
    public async Task Handle_WhenNotBillable_SendsBillRequestNotSent()
    {
        // Arrange
        var request = new ProcessPdfDelivery(EvaluationId, PdfDeliveryId, false);

        // Act
        await CreateSubject().Handle(request, _context);

        // Assert
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>.That.Matches(e =>
                    e.StatusCode == StatusCode.BillRequestNotSent),
                A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _transaction.CommitAsync(A<CancellationToken>._))
            .MustHaveHappened();

        AssertMarkedAsProcessed();
    }

    [Fact]
    public async Task Handle_WhenNotBillable_DoesNotSendBillableEvent()
    {
        // Arrange
        var request = new ProcessPdfDelivery(EvaluationId, PdfDeliveryId, false);

        // Act
        await CreateSubject().Handle(request, _context);

        // Assert
        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>._, A<CancellationToken>._))
            .MustNotHaveHappened();

        Assert.Null(_context.FindSentMessage<BillableEvent>());
        Assert.Null(_context.FindPublishedMessage<BillableEvent>());

        AssertMarkedAsProcessed();
    }

    [Fact]
    public async Task Handle_WhenBillable_ButAlreadyBilled_DoesNotSendBillableEvent()
    {
        // Arrange
        var request = new ProcessPdfDelivery(EvaluationId, PdfDeliveryId, true);

        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>._, A<CancellationToken>._))
            .Returns(new QueryBillRequestSentResult(new BillRequestSent()));

        // Act
        await CreateSubject().Handle(request, _context);

        // Assert
        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>.That.Matches(q =>
                    q.EvaluationId == EvaluationId),
                A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _transaction.CommitAsync(A<CancellationToken>._))
            .MustHaveHappened();

        Assert.Null(_context.FindSentMessage<BillableEvent>());
        Assert.Null(_context.FindPublishedMessage<BillableEvent>());

        AssertMarkedAsProcessed();
    }

    [Fact]
    public async Task Handle_WhenBillable_AndNotBilledYet_SendsBillableEvent()
    {
        // Arrange
        var request = new ProcessPdfDelivery(EvaluationId, PdfDeliveryId, true);

        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>._, A<CancellationToken>._))
            .Returns(new QueryBillRequestSentResult(null));

        // Act
        await CreateSubject().Handle(request, _context);

        // Assert
        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>.That.Matches(q =>
                    q.EvaluationId == EvaluationId),
                A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _mapper.Map<BillableEvent>(A<PdfDeliveredToClient>._))
            .MustHaveHappened();

        Assert.NotNull(_context.FindSentMessage<BillableEvent>());

        A.CallTo(() => _transaction.CommitAsync(A<CancellationToken>._))
            .MustHaveHappened();

        AssertMarkedAsProcessed();
    }
}