using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Signify.AkkaStreams.Kafka.Persistence;
using Signify.Spirometry.Core.Configs.Loopback;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Data;
using Signify.Spirometry.Core.Events;
using Signify.Spirometry.Core.Exceptions;
using Signify.Spirometry.Core.Queries;
using SpiroNsb.SagaCommands;
using SpiroNsb.SagaEvents;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

using CreateFlagCommand = Signify.Spirometry.Core.Commands.CreateFlag;
using StatusCode = Signify.Spirometry.Core.Models.StatusCode;

namespace Signify.Spirometry.Core.Tests.Sagas.Commands;

public class CreateFlagHandlerTests
{
    private readonly IGetLoopbackConfig _getConfig = A.Fake<IGetLoopbackConfig>();
    private readonly ITransactionSupplier _transactionSupplier = A.Fake<ITransactionSupplier>();
    private readonly IMediator _mediator = A.Fake<IMediator>();

    private CreateFlagHandler CreateSubject()
        => new(A.Dummy<ILogger<CreateFlagHandler>>(), _getConfig, _transactionSupplier, _mediator);

    [Fact]
    public async Task Handle_WhenDisabled_DoesNothing()
    {
        // Arrange
        var request = new CreateFlag(default);

        A.CallTo(() => _getConfig.ShouldCreateFlags)
            .Returns(false);

        var context = new TestableMessageHandlerContext();

        // Act
        await Assert.ThrowsAnyAsync<FeatureDisabledException>(async () => await CreateSubject().Handle(request, context));

        // Assert
        A.CallTo(() => _getConfig.ShouldCreateFlags)
            .MustHaveHappened();

        A.CallTo(_mediator)
            .MustNotHaveHappened();

        Assert.Empty(context.SentMessages);
        Assert.Empty(context.PublishedMessages);
    }

    [Fact]
    public async Task Handle_WhenEnabled_AndHoldReleased_DoesNothing()
    {
        // Arrange
        const long evaluationId = 1;

        A.CallTo(() => _getConfig.ShouldCreateFlags)
            .Returns(true);

        A.CallTo(() => _mediator.Send(A<QueryHold>._, A<CancellationToken>._))
            .Returns(new Hold
            {
                ReleasedDateTime = DateTime.UtcNow
            });

        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(new CreateFlag(evaluationId), context);

        // Assert
        A.CallTo(() => _mediator.Send(A<QuerySpirometryExam>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateFlagCommand>._, A<CancellationToken>._))
            .MustNotHaveHappened();

        Assert.Empty(context.SentMessages);
    }

    [Fact]
    public async Task Handle_WhenEnabled_AndFlagAlreadyExists_DoesNotCreateNewFlag()
    {
        // Arrange
        const int evaluationId = 1;
        const int clarificationFlagId = 2;
        var flagCreatedDateTime = DateTime.UtcNow;

        A.CallTo(() => _getConfig.ShouldCreateFlags)
            .Returns(true);

        A.CallTo(() => _mediator.Send(A<QuerySpirometryExam>._, A<CancellationToken>._))
            .Returns(new SpirometryExam
            {
                EvaluationId = evaluationId,
                ClarificationFlag = new ClarificationFlag
                {
                    ClarificationFlagId = clarificationFlagId,
                    CreateDateTime = flagCreatedDateTime
                }
            });

        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(new CreateFlag(evaluationId), context);

        // Assert
        A.CallTo(() => _mediator.Send(A<CreateFlagCommand>._, A<CancellationToken>._))
            .MustNotHaveHappened();

        Assert.Single(context.SentMessages);

        var message = context.FindSentMessage<FlagCreatedEvent>();
        Assert.Equal(evaluationId, message.EvaluationId);
        Assert.Equal(flagCreatedDateTime, message.CreatedDateTime);
        Assert.Equal(clarificationFlagId, message.ClarificationFlagId);
    }

    [Fact]
    public async Task Handle_WhenEnabled_AndFlagAlreadyExists_DoesNotSaveFlagCreatedStatus()
    {
        // Arrange
        const int evaluationId = 1;
        const int clarificationFlagId = 2;
        var flagCreatedDateTime = DateTime.UtcNow;

        A.CallTo(() => _getConfig.ShouldCreateFlags)
            .Returns(true);

        A.CallTo(() => _mediator.Send(A<QuerySpirometryExam>._, A<CancellationToken>._))
            .Returns(new SpirometryExam
            {
                EvaluationId = evaluationId,
                ClarificationFlag = new ClarificationFlag
                {
                    ClarificationFlagId = clarificationFlagId,
                    CreateDateTime = flagCreatedDateTime
                }
            });

        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(new CreateFlag(evaluationId), context);

        // Assert
        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>._, A<CancellationToken>._))
            .MustNotHaveHappened();

        Assert.Single(context.SentMessages);
    }

    [Fact]
    public async Task Handle_WhenEnabled_HappyPath()
    {
        // Arrange
        const int evaluationId = 1;
        const int clarificationFlagId = 2;
        var flagCreatedDateTime = DateTime.UtcNow;

        var exam = new SpirometryExam
        {
            EvaluationId = evaluationId,
            SpirometryExamResult = new SpirometryExamResult()
        };

        A.CallTo(() => _getConfig.ShouldCreateFlags)
            .Returns(true);

        A.CallTo(() => _mediator.Send(A<QuerySpirometryExam>._, A<CancellationToken>._))
            .Returns(exam);

        A.CallTo(() => _mediator.Send(A<CreateFlagCommand>._, A<CancellationToken>._))
            .Returns(new ClarificationFlag
            {
                ClarificationFlagId = clarificationFlagId,
                CreateDateTime = flagCreatedDateTime
            });

        var transaction = A.Fake<IBufferedTransaction>();
        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .Returns(transaction);

        var context = new TestableMessageHandlerContext();

        // Act
        await CreateSubject().Handle(new CreateFlag(evaluationId), context);

        // Assert
        A.CallTo(() => _mediator.Send(A<QuerySpirometryExam>.That.Matches(q =>
                    q.EvaluationId == evaluationId &&
                    q.IncludeResults &&
                    q.IncludeClarificationFlag),
                A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<CreateFlagCommand>.That.Matches(c =>
                    c.Exam == exam &&
                    c.Results == exam.SpirometryExamResult),
                A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => _mediator.Send(A<ExamStatusEvent>.That.Matches(s =>
                    s.Exam == exam &&
                    s.StatusCode == StatusCode.ClarificationFlagCreated),
                A<CancellationToken>._))
            .MustHaveHappened();

        Assert.Single(context.SentMessages);

        var message = context.FindSentMessage<FlagCreatedEvent>();

        Assert.Equal(evaluationId, message.EvaluationId);
        Assert.Equal(clarificationFlagId, message.ClarificationFlagId);
        Assert.Equal(flagCreatedDateTime, message.CreatedDateTime);

        A.CallTo(() => _transactionSupplier.BeginTransaction())
            .MustHaveHappened();
        A.CallTo(() => transaction.CommitAsync(A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => transaction.Dispose())
            .MustHaveHappened();
    }
}