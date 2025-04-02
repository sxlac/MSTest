using AutoMapper;
using FakeItEasy;
using MediatR;
using Microsoft.Extensions.Logging;
using NsbEventHandlers;
using NServiceBus.Testing;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Events;
using SpiroNsb.SagaEvents;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.EventHandlers.Nsb;

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

        A.CallTo(() => _mediator.Send(A<AddHold>._, A<CancellationToken>._))
            .Returns(new AddHoldResponse(new Hold
            {
                EvaluationId = evaluationId,
                HoldId = holdId,
                CreatedDateTime = holdCreatedDateTime
            }, true));

        // Act
        await CreateSubject().Handle(request, _context);

        // Assert
        A.CallTo(() => _mapper.Map<Hold>(request))
            .MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddHold>._, A<CancellationToken>._))
            .MustHaveHappened();

        Assert.Single(_context.SentMessages);

        var actual = _context.FindSentMessage<HoldCreatedEvent>();

        Assert.Equal(evaluationId, actual.EvaluationId);
        Assert.Equal(holdId, actual.HoldId);
        Assert.Equal(holdCreatedDateTime, actual.CreatedDateTime);

        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task Handle_WhenDuplicateHold_DoesNothing()
    {
        // Arrange
        var request = new CDIEvaluationHeldEvent();

        A.CallTo(() => _mediator.Send(A<AddHold>._, A<CancellationToken>._))
            .Returns(new AddHoldResponse(new Hold(), false));

        // Act
        await CreateSubject().Handle(request, _context);

        // Assert
        _transactionSupplier.AssertRollback();

        A.CallTo(() => _mapper.Map<Hold>(request))
            .MustHaveHappened();
        A.CallTo(() => _mediator.Send(A<AddHold>._, A<CancellationToken>._))
            .MustHaveHappened();

        Assert.Empty(_context.SentMessages);
        Assert.Empty(_context.PublishedMessages);
    }
}