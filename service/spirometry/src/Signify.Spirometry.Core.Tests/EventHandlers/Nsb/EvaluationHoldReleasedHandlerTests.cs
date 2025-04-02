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

public sealed class EvaluationHoldReleasedHandlerTests : IDisposable, IAsyncDisposable
{
    private readonly IMediator _mediator = A.Fake<IMediator>();
    private readonly TestableMessageHandlerContext _context = new();
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

    private EvaluationHoldReleasedHandler CreateSubject()
    {
        return new EvaluationHoldReleasedHandler(A.Dummy<ILogger<EvaluationHoldReleasedHandler>>(),
            _mediator, _transactionSupplier);
    }

    [Fact]
    public async Task Handle_WhenHoldNotYetReleased_SendsHoldReleasedEvent()
    {
        // Arrange
        const int evaluationId = 1;
        const int holdId = 2;
        var cdiHoldId = Guid.NewGuid();

        var releasedOn = DateTime.UtcNow;

        var request = new CDIEvaluationHoldReleasedEvent
        {
            EvaluationId = evaluationId,
            HoldId = cdiHoldId,
            ReleasedOn = releasedOn
        };

        A.CallTo(() => _mediator.Send(A<UpdateHold>._, A<CancellationToken>._))
            .Returns(new UpdateHoldResponse(new Hold
            {
                HoldId = holdId,
                CdiHoldId = cdiHoldId,
                EvaluationId = evaluationId,
                ReleasedDateTime = releasedOn
            }, false));

        // Act
        await CreateSubject().Handle(request, _context);

        // Assert
        A.CallTo(() => _mediator.Send(A<UpdateHold>.That.Matches(u =>
                u.CdiHoldId == cdiHoldId &&
                u.ReleasedOn == releasedOn), A<CancellationToken>._))
            .MustHaveHappened();

        Assert.Single(_context.SentMessages);

        var message = _context.FindSentMessage<HoldReleasedEvent>();
        Assert.Equal(evaluationId, message.EvaluationId);
        Assert.Equal(releasedOn, message.CreatedDateTime);
        Assert.Equal(holdId, message.HoldId);

        _transactionSupplier.AssertCommit();
    }

    [Fact]
    public async Task Handle_WhenHoldAlreadyReleased_DoesNothing()
    {
        // Arrange
        A.CallTo(() => _mediator.Send(A<UpdateHold>._, A<CancellationToken>._))
            .Returns(new UpdateHoldResponse(new Hold(), true));

        // Act
        await CreateSubject().Handle(new CDIEvaluationHoldReleasedEvent(), _context);

        // Assert
        _transactionSupplier.AssertRollback();

        Assert.Empty(_context.SentMessages);
        Assert.Empty(_context.PublishedMessages);
    }
}