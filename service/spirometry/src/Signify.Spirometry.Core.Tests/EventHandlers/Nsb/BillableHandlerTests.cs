using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using NsbEventHandlers;
using NServiceBus.Testing;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.Dps.Observability.Library.Services;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Queries;
using SpiroNsbEvents;

using StatusCode = Signify.Spirometry.Core.Models.StatusCode;
using ExamStatusEvent = Signify.Spirometry.Core.Events.ExamStatusEvent;

namespace Signify.Spirometry.Core.Tests.EventHandlers.Nsb;

public class BillableHandlerTests
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly ITransactionSupplier _transactionSupplier = A.Fake<ITransactionSupplier>();
    private readonly IPublishObservability _publishObservability = A.Fake<IPublishObservability>();

    private BillableHandler CreateSubject()
        => new BillableHandler(A.Dummy<ILogger<BillableHandler>>(), A.Dummy<IMapper>(), _mediator, _transactionSupplier, _publishObservability);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_AlwaysSendsBillableEventReceivedStatus(bool billRequestSentExists)
    {
        const int evaluationId = 1;

        var request = new BillableEvent
        {
            EventId = Guid.NewGuid(),
            EvaluationId = evaluationId,
            BillableDate = DateTime.UtcNow
        };

        A.CallTo(() => _mediator.Send(A<QuerySpirometryExam>._, A<CancellationToken>._))
            .Returns(new SpirometryExam { EvaluationId = evaluationId });

        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>._, A<CancellationToken>._))
            .Returns(new QueryBillRequestSentResult(billRequestSentExists ? new BillRequestSent() : null));

        var transaction = A.Fake<IBufferedTransaction>();
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .Returns(transaction);

        await CreateSubject().Handle(request, new TestableMessageHandlerContext());

        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>.That.Matches(s =>
                    s.EventId == request.EventId &&
                    s.Exam.EvaluationId == evaluationId &&
                    s.StatusDateTime == request.BillableDate &&
                    s.StatusCode == StatusCode.BillableEventReceived),
                A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .MustHaveHappened();
        A.CallTo(() => transaction.CommitAsync(A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => transaction.Dispose())
            .MustHaveHappened();
        A.CallTo(() => _publishObservability.Commit())
            .MustHaveHappened();
    }

    [Fact]
    public async Task Handle_WhenBillRequestAlreadySent_DoesNotCreateNewBill()
    {
        const int evaluationId = 1;

        var request = new BillableEvent
        {
            EvaluationId = evaluationId
        };

        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>
                    .That.Matches(q => q.EvaluationId == evaluationId),
                A<CancellationToken>._))
            .Returns(new QueryBillRequestSentResult(new BillRequestSent()));

        var transaction = A.Fake<IBufferedTransaction>();
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .Returns(transaction);

        await CreateSubject().Handle(request, new TestableMessageHandlerContext());

        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<CreateBill>._, A<CancellationToken>._))
            .MustNotHaveHappened();

        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>.That.Matches(s =>
                    s.StatusCode == StatusCode.BillRequestSent),
                A<CancellationToken>._))
            .MustNotHaveHappened();

        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .MustHaveHappened();
        A.CallTo(() => transaction.CommitAsync(A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => transaction.Dispose())
            .MustHaveHappened();
        A.CallTo(() => _publishObservability.Commit())
            .MustHaveHappened();
    }

    [Fact]
    public async Task Handle_WhenBillRequestNotAlreadySent_CreatesNewBill()
    {
        const int evaluationId = 1;

        var request = new BillableEvent
        {
            EventId = Guid.NewGuid(),
            EvaluationId = evaluationId
        };

        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>
                    .That.Matches(q => q.EvaluationId == evaluationId),
                A<CancellationToken>._))
            .Returns(new QueryBillRequestSentResult(null));

        A.CallTo(() => _mediator.Send(A<CreateBill>._, A<CancellationToken>._))
            .Returns(new BillRequestSent());

        await CreateSubject().Handle(request, new TestableMessageHandlerContext());

        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<CreateBill>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _publishObservability.Commit())
            .MustHaveHappened();}

    [Fact]
    public async Task Handle_WhenCreatesBill_SendsBillRequestSent()
    {
        var request = new BillableEvent
        {
            EventId = Guid.NewGuid(),
            EvaluationId = 1
        };

        var statusTime = DateTime.UtcNow;

        A.CallTo(() => _mediator.Send(A<QueryBillRequestSent>._, A<CancellationToken>._))
            .Returns(new QueryBillRequestSentResult(null));

        A.CallTo(() => _mediator.Send(A<CreateBill>._, A<CancellationToken>._))
            .Returns(new BillRequestSent { CreatedDateTime = statusTime });

        var transaction = A.Fake<IBufferedTransaction>();
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .Returns(transaction);

        await CreateSubject().Handle(request, new TestableMessageHandlerContext());

        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<CreateBill>._, A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>.That.Matches(s =>
                    s.StatusCode == StatusCode.BillRequestSent),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .MustHaveHappened();
        A.CallTo(() => transaction.CommitAsync(A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => transaction.Dispose())
            .MustHaveHappened();
        A.CallTo(() => _publishObservability.Commit())
            .MustHaveHappened();}
}