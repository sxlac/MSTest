using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus.Testing;
using NServiceBus;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Configs.Loopback;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Exceptions;
using Signify.Spirometry.Core.Infrastructure;
using Signify.Spirometry.Core.Queries;
using SpiroEvents;
using SpiroNsb.SagaEvents;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.EventHandlers.Nsb;

public class OverreadProcessedByVendorHandlerTests
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly ITransactionSupplier _transactionSupplier = A.Fake<ITransactionSupplier>();
    private readonly IApplicationTime _applicationTime = new FakeApplicationTime();

    private OverreadProcessedByVendorHandler CreateSubject(IGetLoopbackConfig config = null)
        => new(A.Dummy<ILogger<OverreadProcessedByVendorHandler>>(), _mediator, _mapper, _transactionSupplier, _applicationTime, config ?? A.Dummy<IGetLoopbackConfig>());

    [Fact]
    public async Task Handle_HappyPath()
    {
        // Arrange
        const long evaluationId = 1;
        const int overreadResultId = 2;

        var transaction = A.Fake<IBufferedTransaction>();
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .Returns(transaction);

        A.CallTo(() => _mediator.Send(A<QueryOverreadResult>._, A<CancellationToken>._))
            .Returns((OverreadResult) null);

        var overreadResult = new OverreadResult();
        A.CallTo(() => _mapper.Map<OverreadResult>(A<OverreadProcessed>._))
            .Returns(overreadResult);

        A.CallTo(() => _mediator.Send(A<AddOverreadResult>._, A<CancellationToken>._))
            .Returns(new OverreadResult
            {
                OverreadResultId = overreadResultId,
                CreatedDateTime = _applicationTime.UtcNow()
            });

        A.CallTo(() => _mediator.Send(A<QueryEvaluationId>._, A<CancellationToken>._))
            .Returns(evaluationId);

        var context = new TestableMessageHandlerContext();

        var subject = CreateSubject();

        // Act
        await subject.Handle(new OverreadProcessed(), context);

        // Assert
        A.CallTo(() => _mapper.Map<OverreadResult>(A<OverreadProcessed>.That.IsNotNull()))
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<AddOverreadResult>.That.Matches(a =>
                    a.Result == overreadResult &&
                    a.Result.CreatedDateTime == _applicationTime.UtcNow()),
                A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .MustHaveHappened();

        Assert.Single(context.SentMessages);

        var message = context.FindSentMessage<OverreadReceivedEvent>();

        Assert.NotNull(message);
        Assert.Equal(evaluationId, message.EvaluationId);
        Assert.Equal(overreadResultId, message.OverreadResultId);
        Assert.Equal(_applicationTime.UtcNow(), message.CreatedDateTime);

        A.CallTo(() => transaction.CommitAsync(A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => transaction.Dispose())
            .MustHaveHappened();
    }

    [Fact]
    public async Task Handle_WhenUnableToDetermineEvaluationId_AndCannotRetry_Throws()
    {
        // Arrange
        var request = new OverreadProcessed();

        var transaction = A.Fake<IBufferedTransaction>();
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .Returns(transaction);

        A.CallTo(() => _mediator.Send(A<QueryOverreadResult>._, A<CancellationToken>._))
            .Returns((OverreadResult) null);

        A.CallTo(() => _mediator.Send(A<QueryEvaluationId>._, A<CancellationToken>._))
            .Throws(() => new EvaluationNotFoundException(default));

        var config = A.Fake<IGetLoopbackConfig>();
        A.CallTo(() => config.CanRetryOverreadEvaluationLookup(A<DateTimeOffset>._))
            .Returns(false);

        var context = new TestableMessageHandlerContext();

        // Act
        // Assert
        await Assert.ThrowsAnyAsync<EvaluationNotFoundException>(async () => await CreateSubject(config).Handle(request, context));

        A.CallTo(() => transaction.CommitAsync(A<CancellationToken>._))
            .MustNotHaveHappened();

        Assert.Empty(context.SentMessages);
    }

    [Fact]
    public async Task Handle_WhenUnableToDetermineEvaluationId_AndCanRetry_SendsDelayedRetry()
    {
        // Arrange
        var request = new OverreadProcessed
        {
            ReceivedDateTime = DateTimeOffset.UtcNow
        };

        var delay = TimeSpan.FromSeconds(10);

        var transaction = A.Fake<IBufferedTransaction>();
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .Returns(transaction);

        A.CallTo(() => _mediator.Send(A<QueryOverreadResult>._, A<CancellationToken>._))
            .Returns((OverreadResult) null);

        A.CallTo(() => _mediator.Send(A<QueryEvaluationId>._, A<CancellationToken>._))
            .Throws(() => new EvaluationNotFoundException(default));

        var config = A.Fake<IGetLoopbackConfig>();
        A.CallTo(() => config.CanRetryOverreadEvaluationLookup(A<DateTimeOffset>._))
            .Returns(true);
        A.CallTo(() => config.OverreadEvaluationLookupRetryDelay)
            .Returns(delay);

        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject(config).Handle(request, context);

        // Assert
        A.CallTo(() => config.CanRetryOverreadEvaluationLookup(A<DateTimeOffset>.That.Matches(
                d => d == request.ReceivedDateTime)))
            .MustHaveHappened();

        A.CallTo(() => transaction.CommitAsync(A<CancellationToken>._))
            .MustNotHaveHappened();

        Assert.Single(context.SentMessages);

        var message = context.SentMessages.First();

        Assert.Equal(delay, message.Options.GetDeliveryDelay());
        Assert.Same(request, message.Message);
    }

    [Fact]
    public async Task Handle_WhenAddOverreadResultThrows_DoesNotCommitTransaction()
    {
        // Arrange
        var transaction = A.Fake<IBufferedTransaction>();
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .Returns(transaction);

        A.CallTo(() => _mediator.Send(A<QueryOverreadResult>._, A<CancellationToken>._))
            .Returns((OverreadResult) null);

        A.CallTo(() => _mediator.Send(A<AddOverreadResult>._, A<CancellationToken>._))
            .Throws<Exception>();

        var subject = CreateSubject();

        // Act
        await Assert.ThrowsAnyAsync<Exception>(async () => await subject.Handle(new OverreadProcessed(), default));

        // Assert
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .MustHaveHappened();

        A.CallTo(() => transaction.CommitAsync(A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => transaction.Dispose())
            .MustHaveHappened();
    }

    [Fact]
    public async Task Handle_WhenOverreadAlreadyExistsForAppointment_DoesNothing()
    {
        // Arrange
        const long appointmentId = 1;

        A.CallTo(() => _mediator.Send(A<QueryOverreadResult>._, A<CancellationToken>._))
            .Returns(new OverreadResult());

        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(new OverreadProcessed {AppointmentId = appointmentId}, context);

        // Assert
        A.CallTo(() => _mediator.Send(A<QueryOverreadResult>.That.Matches(q =>
                    q.AppointmentId == appointmentId),
                A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<AddOverreadResult>._, A<CancellationToken>._))
            .MustNotHaveHappened();

        Assert.Empty(context.SentMessages);
        Assert.Empty(context.PublishedMessages);
    }

    [Theory]
    [MemberData(nameof(Handle_Normality_TestData))]
    public async Task Handle_Normality_Tests(string obstructionPerOverread, NormalityIndicator expectedNormality)
    {
        var source = new OverreadProcessed
        {
            ObstructionPerOverread = obstructionPerOverread
        };

        A.CallTo(() => _mediator.Send(A<QueryOverreadResult>._, A<CancellationToken>._))
            .Returns((OverreadResult) null);

        var subject = CreateSubject();

        await subject.Handle(source, new TestableMessageHandlerContext());

        A.CallTo(() => _mediator.Send(A<AddOverreadResult>.That.Matches(a =>
                    a.Result.NormalityIndicatorId == expectedNormality.NormalityIndicatorId),
                A<CancellationToken>._))
            .MustHaveHappened();
    }

    public static IEnumerable<object[]> Handle_Normality_TestData()
    {
        yield return
        [
            "YeS",
            NormalityIndicator.Abnormal
        ];

        yield return
        [
            "nO",
            NormalityIndicator.Normal
        ];

        yield return
        [
            "undetermined",
            NormalityIndicator.Undetermined
        ];

        yield return
        [
            "Indeterminate",
            NormalityIndicator.Undetermined
        ];

        yield return
        [
            "inconclusive",
            NormalityIndicator.Undetermined
        ];

        yield return
        [
            null,
            NormalityIndicator.Undetermined
        ];

        yield return
        [
            string.Empty,
            NormalityIndicator.Undetermined
        ];

        yield return
        [
            "some invalid value",
            NormalityIndicator.Undetermined
        ];
    }
}