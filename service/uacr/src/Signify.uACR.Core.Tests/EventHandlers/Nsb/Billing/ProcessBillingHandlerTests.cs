using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Data;
using Signify.uACR.Core.Queries;
using System.Threading.Tasks;
using System.Threading;
using System;
using Signify.Dps.Observability.Library.Services;
using Signify.uACR.Core.Constants;
using UacrNsbEvents;
using Xunit;

namespace Signify.uACR.Core.Tests.EventHandlers.Nsb;

public class ProcessBillingHandlerTests
{
    private const long EvaluationId = 1;
    private const int PdfDeliveryId = 2;

    private readonly ITransactionSupplier _transactionSupplier = A.Fake<ITransactionSupplier>();
    private readonly IBufferedTransaction _transaction = A.Fake<IBufferedTransaction>();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IMessageHandlerContext _fakeContext = A.Fake<IMessageHandlerContext>();
    private readonly FakeApplicationTime _applicationTime = new();
    private readonly Guid _eventId = Guid.NewGuid();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();

    public ProcessBillingHandlerTests()
    {
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .Returns(_transaction);
    }

    private ProcessBillingHandler CreateSubject()
        => new(A.Dummy<ILogger<ProcessBillingHandler>>(), _transactionSupplier, _mediator, _publishObservability, _applicationTime);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WhetherBillableOrNot_SendsClientPdfDeliveredStatus(bool isBillable)
    {
        // Arrange
        var request = new ProcessBillingEvent(_eventId, EvaluationId, PdfDeliveryId, isBillable, _applicationTime.UtcNow(), ProductCodes.uACR_RcmBilling);

        // Act
        await CreateSubject().Handle(request, _fakeContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<QueryPdfDeliveredToClient>.That.Matches(q =>
                    q.EvaluationId == EvaluationId),
                A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .MustHaveHappened();
        A.CallTo(() => _transaction.CommitAsync(A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _transaction.Dispose())
            .MustHaveHappened();
    }

    [Fact]
    public async Task Handle_WhenNotBillable_SendsBillRequestNotSent()
    {
        // Arrange
        var request = new ProcessBillingEvent(_eventId, EvaluationId, PdfDeliveryId, false, _applicationTime.UtcNow(), ProductCodes.uACR_RcmBilling);

        // Act
        await CreateSubject().Handle(request, _fakeContext);

        // Assert
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .MustHaveHappened();
        
        A.CallTo(() => _fakeContext.Send(A<ExamStatusEvent>.That.Matches(e => e.StatusCode == ExamStatusCode.BillRequestNotSent), A<SendOptions>._))
            .MustHaveHappened(1, Times.Exactly);

        A.CallTo(() => _transaction.CommitAsync(A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Fact]
    public async Task Handle_WhenNotBillable_DoesNotSendBillableEvent()
    {
        // Arrange
        var request = new ProcessBillingEvent(_eventId, EvaluationId, PdfDeliveryId, false, _applicationTime.UtcNow(), ProductCodes.uACR_RcmBilling);

        // Act
        await CreateSubject().Handle(request, _fakeContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<QueryBillRequests>._, A<CancellationToken>._))
            .MustNotHaveHappened();

        A.CallTo(() => _fakeContext.Send(A<CreateBillEvent>.That.Matches(e => e.EventId == _eventId), A<SendOptions>._))
            .MustNotHaveHappened();

    }

    [Fact]
    public async Task Handle_WhenBillable_ButAlreadyBilled_DoesNotSendBillableEvent()
    {
        // Arrange
        var request = new ProcessBillingEvent(_eventId, EvaluationId, PdfDeliveryId, true, _applicationTime.UtcNow(), ProductCodes.uACR_RcmBilling);

        A.CallTo(() => _mediator.Send(A<QueryBillRequests>._, A<CancellationToken>._))
            .Returns(new QueryBillRequestsResult(new BillRequest()));

        // Act
        await CreateSubject().Handle(request, _fakeContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<QueryBillRequests>.That.Matches(q =>
                    q.EvaluationId == EvaluationId),
                A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _transaction.CommitAsync(A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _fakeContext.Send(A<CreateBillEvent>.That.Matches(e => e.EventId == _eventId), A<SendOptions>._))
            .MustNotHaveHappened();
    }

    [Theory]
    [InlineData(ProductCodes.uACR_RcmBilling)]
    [InlineData(ProductCodes.UAcrRcmBillingLeft)]
    [InlineData(ProductCodes.UAcrRcmBillingResults)]
    public async Task Handle_WhenBillable_AndNotBilledYet_SendsBillableEvent(string rcmProductCode)
    {
        // Arrange
        var request = new ProcessBillingEvent(_eventId, EvaluationId, PdfDeliveryId, true, _applicationTime.UtcNow(), rcmProductCode);

        A.CallTo(() => _mediator.Send(A<QueryBillRequests>._, A<CancellationToken>._))
            .Returns(new QueryBillRequestsResult(null));

        // Act
        await CreateSubject().Handle(request, _fakeContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<QueryBillRequests>.That.Matches(q =>
                    q.EvaluationId == EvaluationId),
                A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _fakeContext.Send(A<CreateBillEvent>.That.Matches(e => e.EventId == _eventId), A<SendOptions>._))
            .MustHaveHappened(1, Times.Exactly);

        A.CallTo(() => _transaction.CommitAsync(A<CancellationToken>._))
            .MustHaveHappened();
    }
}