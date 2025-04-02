using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus.Testing;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.uACR.Core.Commands;
using Signify.uACR.Core.Data.Entities;
using Signify.uACR.Core.Data;
using Signify.uACR.Core.Infrastructure;
using Signify.uACR.Core.Queries;
using System.Threading.Tasks;
using System.Threading;
using System;
using Signify.Dps.Observability.Library.Services;
using Signify.uACR.Core.Constants;
using UacrNsbEvents;
using Xunit;

namespace Signify.uACR.Core.Tests.EventHandlers.Nsb;

public class BillRequestHandlerTests
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly ITransactionSupplier _transactionSupplier = A.Fake<ITransactionSupplier>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly IApplicationTime _applicationTime = new FakeApplicationTime();

    private BillRequestHandler CreateSubject()
        => new(A.Dummy<ILogger<BillRequestHandler>>(),
            _mediator, _transactionSupplier, _publishObservability, _applicationTime);

    [Fact]
    public async Task Handle_WhenAlreadyHasBillRequestInDb_DoesNothing()
    {
        // Arrange
        const long evaluationId = 1;
        const int examId = 1;
        var billId = Guid.NewGuid();
        var request = new BillRequestEvent
        {
            EvaluationId = evaluationId,
            ExamId = examId,
            BillId = billId,
            RcmProductCode = ProductCodes.uACR_RcmBilling
        };
        A.CallTo(() => _mediator.Send(A<QueryBillRequests>._, A<CancellationToken>._))
            .Returns(new QueryBillRequestsResult(new BillRequest()));
        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        A.CallTo(() => _mediator.Send(A<QueryBillRequests>.That.Matches(q =>
                    q.EvaluationId == evaluationId),
                A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddOrUpdateBillRequest>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Handle_WhenNoBillRequestInDb_HappyPath()
    {
        // Arrange
        const int examId = 1;
        var billId = Guid.NewGuid();
        
        A.CallTo(() => _mediator.Send(A<QueryBillRequests>._, A<CancellationToken>._))
            .Returns(new QueryBillRequestsResult(null));
        var transaction = A.Fake<IBufferedTransaction>();
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .Returns(transaction);
        A.CallTo(() => _mediator.Send(A<AddOrUpdateBillRequest>._, A<CancellationToken>._))
            .Returns(new BillRequest
            {
                ExamId = examId,
                BillId = billId,
                BillingProductCode = ProductCodes.uACR_RcmBilling
            });
        var request = new BillRequestEvent();
        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddOrUpdateBillRequest>._, A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => transaction.CommitAsync(A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => transaction.Dispose())
            .MustHaveHappened();
    }
}