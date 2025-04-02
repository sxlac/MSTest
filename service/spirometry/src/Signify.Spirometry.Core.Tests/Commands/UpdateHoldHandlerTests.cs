using FakeItEasy;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Exceptions;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Commands;

public sealed class UpdateHoldHandlerTests : IDisposable, IAsyncDisposable
{
    private readonly MockDbFixture _dbFixture = new();

    public void Dispose()
    {
        _dbFixture.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _dbFixture.DisposeAsync();
    }

    private UpdateHoldHandler CreateSubject()
        => new(A.Dummy<ILogger<UpdateHoldHandler>>(), _dbFixture.SharedDbContext);

    [Fact]
    public async Task Handle_WithNotReleased_UpdatesHold()
    {
        // Arrange
        const int evaluationId = 1;
        const int holdId = 1;
        var cdiHoldId = Guid.NewGuid();

        var releasedDateTime = DateTime.UtcNow;

        var existingHold = new Hold
        {
            EvaluationId = evaluationId,
            HoldId = holdId,
            CdiHoldId = cdiHoldId
        };

        await _dbFixture.SharedDbContext.Holds.AddAsync(existingHold);
        await _dbFixture.SharedDbContext.SaveChangesAsync();

        var request = new UpdateHold(cdiHoldId, evaluationId, releasedDateTime);

        var subject = CreateSubject();

        // Act
        var actualResult = await subject.Handle(request, CancellationToken.None);

        // Assert
        Assert.False(actualResult.IsNoOp);
        Assert.Equal(releasedDateTime, actualResult.Hold.ReleasedDateTime);
    }

    [Fact]
    public async Task Handle_WhenHoldAlreadyReleased_DoesNothing()
    {
        // Arrange
        const int evaluationId = 1;
        var cdiHoldId = Guid.NewGuid();
        var previouslyReleasedTimestamp = DateTime.UtcNow;

        var hold = new Hold
        {
            EvaluationId = evaluationId,
            CdiHoldId = cdiHoldId,
            ReleasedDateTime = previouslyReleasedTimestamp
        };

        await _dbFixture.SharedDbContext.Holds.AddAsync(hold);
        await _dbFixture.SharedDbContext.SaveChangesAsync();

        var request = new UpdateHold(cdiHoldId, evaluationId, DateTime.UtcNow);

        var subject = CreateSubject();

        // Act
        var result = await subject.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsNoOp);
        Assert.Equal(previouslyReleasedTimestamp, result.Hold.ReleasedDateTime);
        Assert.Equal(hold, result.Hold);
    }

    [Fact]
    public async Task Handle_WhenHoldNotFoundForEvaluation_Throws()
    {
        // Arrange
        var request = new UpdateHold(Guid.NewGuid(), 1, DateTime.UtcNow);

        var subject = CreateSubject();

        // Act
        // Assert
        await Assert.ThrowsAnyAsync<HoldNotFoundException>(async () =>
            await subject.Handle(request, CancellationToken.None));
    }
}