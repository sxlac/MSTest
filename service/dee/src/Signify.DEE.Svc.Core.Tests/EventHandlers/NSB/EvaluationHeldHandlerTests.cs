using AutoMapper;
using FakeItEasy;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus;
using NServiceBus.Testing;
using Signify.DEE.Svc.Core.Data.Entities;
using Signify.DEE.Svc.Core.EventHandlers.Nsb;
using Signify.DEE.Svc.Core.Events;
using Signify.DEE.Svc.Core.Messages.Commands;
using Signify.DEE.Svc.Core.Messages.Queries;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.EventHandlers.Nsb;

public sealed class EvaluationHeldHandlerTests : IDisposable, IAsyncDisposable
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly TestableMessageHandlerContext _context = new();
    private readonly IMapper _mapper = A.Fake<IMapper>();
    private readonly FakeTransactionSupplier _transactionSupplier = new();

    private readonly MockDbFixture _dbFixture = new();

    public void Dispose()
    {
        _dbFixture.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _dbFixture.DisposeAsync();
    }

    private EvaluationHeldHandler CreateSubject()
    {
        return new EvaluationHeldHandler(A.Dummy<ILogger<EvaluationHeldHandler>>(),
            _mediator, _mapper, _transactionSupplier);
    }

    [Fact]
    public async Task Handle_WhenNotDuplicate_SendsHoldCreatedEvent()
    {
        // Arrange
        const int evaluationId = 1;
        const int holdId = 2;
        var holdCreatedDateTime = DateTime.UtcNow;

        var request = new CDIEvaluationHeldEvent();

        A.CallTo(() => _mediator.Send(A<CreateHold>._, A<CancellationToken>._))
            .Returns(new CreateHoldResponse(new Hold
            {
                EvaluationId = evaluationId,
                HoldId = holdId,
                CreatedDateTime = holdCreatedDateTime
            }, true));

        Exam ex = null;
        A.CallTo(() => _mediator.Send(A<GetExamByEvaluation>._, A<CancellationToken>._))
            .Returns(ex);

        // Act
        await CreateSubject().Handle(request, _context);

        // Assert
        A.CallTo(() => _mapper.Map<Hold>(request))
            .MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateHold>._, A<CancellationToken>._))
            .MustHaveHappened();
        _context.SentMessages.Length.Should().Be(0);

        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task Handle_WhenDuplicateHold_DoesNothing()
    {
        // Arrange
        var request = new CDIEvaluationHeldEvent();

        A.CallTo(() => _mediator.Send(A<CreateHold>._, A<CancellationToken>._))
            .Returns(new CreateHoldResponse(new Hold(), false));

        // Act
        await CreateSubject().Handle(request, _context);

        // Assert
        _transactionSupplier.AssertRollback();

        A.CallTo(() => _mapper.Map<Hold>(request))
            .MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateHold>._, A<CancellationToken>._))
            .MustHaveHappened();

        Assert.Empty(_context.SentMessages);
        Assert.Empty(_context.PublishedMessages);
    }

    [Fact]
    public async Task Handle_WhenNotPerformedExists_SendsHoldReleasedEvent()
    {
        // Arrange
        const int evaluationId = 1;
        const int holdId = 2;
        var holdCreatedDateTime = DateTime.UtcNow;

        var request = new CDIEvaluationHeldEvent();

        A.CallTo(() => _mediator.Send(A<CreateHold>._, A<CancellationToken>._))
            .Returns(new CreateHoldResponse(new Hold
            {
                EvaluationId = evaluationId,
                HoldId = holdId,
                CreatedDateTime = holdCreatedDateTime
            }, true));

        A.CallTo(() => _mediator.Send(A<GetExamByEvaluation>._, A<CancellationToken>._))
            .Returns(new Exam() { ExamId = 123 });
        A.CallTo(() => _mediator.Send(A<GetNotPerformedExamByExamId>._, A<CancellationToken>._))
            .Returns(new DeeNotPerformed() { ExamId = 123 });
        ExamResult res = null;
        A.CallTo(() => _mediator.Send(A<GetExamResultByExamId>._, A<CancellationToken>._))
            .Returns(res);

        // Act
        await CreateSubject().Handle(request, _context);

        // Assert
        A.CallTo(() => _mapper.Map<Hold>(request))
            .MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateHold>._, A<CancellationToken>._))
            .MustHaveHappened();
        _context.SentMessages.Length.Should().Be(1);
        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task Handle_WhenExamResultExists_SendsHoldReleasedEvent()
    {
        // Arrange
        const int evaluationId = 1;
        const int holdId = 2;
        var holdCreatedDateTime = DateTime.UtcNow;

        var request = new CDIEvaluationHeldEvent();

        A.CallTo(() => _mediator.Send(A<CreateHold>._, A<CancellationToken>._))
            .Returns(new CreateHoldResponse(new Hold
            {
                EvaluationId = evaluationId,
                HoldId = holdId,
                CreatedDateTime = holdCreatedDateTime
            }, true));

        A.CallTo(() => _mediator.Send(A<GetExamByEvaluation>._, A<CancellationToken>._))
            .Returns(new Exam() { ExamId = 123 });
        DeeNotPerformed notPerf = null;
        A.CallTo(() => _mediator.Send(A<GetNotPerformedExamByExamId>._, A<CancellationToken>._))
            .Returns(notPerf);
        A.CallTo(() => _mediator.Send(A<GetExamResultByExamId>._, A<CancellationToken>._))
            .Returns(new ExamResult() { ExamId = 123});

        // Act
        await CreateSubject().Handle(request, _context);

        // Assert
        A.CallTo(() => _mapper.Map<Hold>(request))
            .MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<CreateHold>._, A<CancellationToken>._))
            .MustHaveHappened();
        _context.SentMessages.Length.Should().Be(1);
        _transactionSupplier.AssertCommit();
    }
}