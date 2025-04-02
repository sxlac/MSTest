using System;
using System.Threading;
using System.Threading.Tasks;
using EgfrNsbEvents;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.eGFR.Core.Constants;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Queries;
using Xunit;

namespace Signify.eGFR.Core.Tests.EventHandlers.Nsb.Billing;

public class ProcessBillingHandlerTests
{
    private const long EvaluationId = 1;
    private const int PdfDeliveryId = 2;

    private readonly ITransactionSupplier _transactionSupplier = A.Fake<ITransactionSupplier>();
    private readonly IBufferedTransaction _transaction = A.Fake<IBufferedTransaction>();
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IMessageHandlerContext _fakeContext = A.Fake<IMessageHandlerContext>();
    private readonly FakeApplicationTime _applicationTime = new();
    
    private Guid _eventId = Guid.NewGuid();

    public ProcessBillingHandlerTests()
    {
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .Returns(_transaction);
    }

    private ProcessBillingHandler CreateSubject()
        => new(A.Dummy<ILogger<ProcessBillingHandler>>(), _transactionSupplier, _mediator);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WhetherBillableOrNot_SendsClientPdfDeliveredStatus(bool isBillable)
    {
        // Arrange
        var request = new ProcessBillingEvent(_eventId, EvaluationId, PdfDeliveryId, isBillable, _applicationTime.UtcNow(), ProductCodes.eGFR_RcmBilling);

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
        var request = new ProcessBillingEvent(_eventId, EvaluationId, PdfDeliveryId, false, _applicationTime.UtcNow(), ProductCodes.eGFR_RcmBilling);

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
        var request = new ProcessBillingEvent(_eventId, EvaluationId, PdfDeliveryId, false, _applicationTime.UtcNow(), ProductCodes.eGFR_RcmBilling);

        // Act
        await CreateSubject().Handle(request, _fakeContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>._, A<CancellationToken>._))
            .MustNotHaveHappened();

        A.CallTo(() => _fakeContext.Send(A<CreateBillEvent>.That.Matches(e => e.EventId == _eventId), A<SendOptions>._))
            .MustNotHaveHappened();

    }

    [Fact]
    public async Task Handle_WhenBillable_ButAlreadyBilled_DoesNotSendBillableEvent()
    {
        // Arrange
        var request = new ProcessBillingEvent(_eventId, EvaluationId, PdfDeliveryId, true, _applicationTime.UtcNow(), ProductCodes.eGFR_RcmBilling);

        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>._, A<CancellationToken>._))
            .Returns(new QueryBillRequestSentResult(new BillRequestSent()));

        // Act
        await CreateSubject().Handle(request, _fakeContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>.That.Matches(q =>
                    q.EvaluationId == EvaluationId),
                A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _transaction.CommitAsync(A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _fakeContext.Send(A<CreateBillEvent>.That.Matches(e => e.EventId == _eventId), A<SendOptions>._))
            .MustNotHaveHappened();
    }

    [Theory]
    [InlineData(ProductCodes.eGFR_RcmBilling)]
    [InlineData(ProductCodes.EGfrRcmBillingLeft)]
    [InlineData(ProductCodes.EGfrRcmBillingResults)]
    public async Task Handle_WhenBillable_AndNotBilledYet_SendsBillableEvent(string rcmProductCode)
    {
        // Arrange
        var request = new ProcessBillingEvent(_eventId, EvaluationId, PdfDeliveryId, true, _applicationTime.UtcNow(), rcmProductCode);

        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>._, A<CancellationToken>._))
            .Returns(new QueryBillRequestSentResult(null));

        // Act
        await CreateSubject().Handle(request, _fakeContext);

        // Assert
        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>.That.Matches(q =>
                    q.EvaluationId == EvaluationId),
                A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _fakeContext.Send(A<CreateBillEvent>.That.Matches(e => e.EventId == _eventId), A<SendOptions>._))
            .MustHaveHappened(1, Times.Exactly);

        A.CallTo(() => _transaction.CommitAsync(A<CancellationToken>._))
            .MustHaveHappened();
    }
}