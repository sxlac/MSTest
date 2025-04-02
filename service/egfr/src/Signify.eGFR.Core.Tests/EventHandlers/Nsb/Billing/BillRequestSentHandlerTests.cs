using System;
using System.Threading;
using System.Threading.Tasks;
using EgfrNsbEvents;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus.Testing;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.Dps.Observability.Library.Services;
using Signify.eGFR.Core.Commands;
using Signify.eGFR.Core.Constants;
using Signify.eGFR.Core.Data;
using Signify.eGFR.Core.Data.Entities;
using Signify.eGFR.Core.Queries;
using Xunit;

namespace Signify.eGFR.Core.Tests.EventHandlers.Nsb.Billing;

public class BillRequestSentHandlerTests
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly ITransactionSupplier _transactionSupplier = A.Fake<ITransactionSupplier>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();
    private readonly FakeApplicationTime _applicationTime = new();

    private BillRequestSentHandler CreateSubject()
        => new(A.Dummy<ILogger<BillRequestSentHandler>>(),
            _mediator, _transactionSupplier, _publishObservability, _applicationTime);

    [Fact]
    public async Task Handle_WhenAlreadyHasBillRequestSentInDb_DoesNothing()
    {
        // Arrange
        const long evaluationId = 1;
        const int examId = 1;
        var billId = Guid.NewGuid();
        var request = new BillRequestSentEvent
        {
            EvaluationId = evaluationId,
            ExamId = examId,
            BillId = billId,
            RcmProductCode = ProductCodes.eGFR_RcmBilling
        };
        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>._, A<CancellationToken>._))
            .Returns(new QueryBillRequestSentResult(new BillRequestSent()));
        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>.That.Matches(q =>
                    q.EvaluationId == evaluationId),
                A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddOrUpdateBillRequestSent>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task Handle_WhenNoBillRequestSentInDb_HappyPath()
    {
        // Arrange
        const int examId = 1;
        var billId = Guid.NewGuid();
        var billingProductCode = ProductCodes.eGFR_RcmBilling;
        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>._, A<CancellationToken>._))
            .Returns(new QueryBillRequestSentResult(null));
        var transaction = A.Fake<IBufferedTransaction>();
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .Returns(transaction);
        A.CallTo(() => _mediator.Send(A<AddOrUpdateBillRequestSent>._, A<CancellationToken>._))
            .Returns(new BillRequestSent
            {
                ExamId = examId,
                BillId = billId,
                BillingProductCode = billingProductCode
            });
        var request = new BillRequestSentEvent();
        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(request, context);

        // Assert
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddOrUpdateBillRequestSent>._, A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => transaction.CommitAsync(A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => transaction.Dispose())
            .MustHaveHappened();
    }
}