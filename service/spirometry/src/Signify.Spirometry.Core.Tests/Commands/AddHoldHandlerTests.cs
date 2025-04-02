using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Data.Entities;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Commands;

public sealed class AddHoldHandlerTests : IDisposable, IAsyncDisposable
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

    private AddHoldHandler CreateSubject()
        => new(A.Dummy<ILogger<AddHoldHandler>>(), _dbFixture.SharedDbContext);

    [Fact]
    public async Task Handle_WithNewHold_AddsHoldToDatabase()
    {
        // Arrange
        const int evaluationId = 1;

        var hold = new Hold
        {
            EvaluationId = evaluationId
        };

        var request = new AddHold(hold);

        var subject = CreateSubject();

        var existingHoldCount = await _dbFixture.SharedDbContext.Holds.CountAsync();

        // Act
        var actualResult = await subject.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(actualResult.IsNew);
        Assert.Equal(hold, actualResult.Hold);

        Assert.Equal(existingHoldCount + 1, await _dbFixture.SharedDbContext.Holds.CountAsync());

        var insertedHold = await _dbFixture.SharedDbContext.Holds.SingleOrDefaultAsync(each => each.EvaluationId == evaluationId);

        Assert.Equal(hold, insertedHold);
    }

    [Fact]
    public async Task Handle_WithDuplicateHold_DoesNothing()
    {
        // Arrange
        const int evaluationId = 1;

        var existingHold = new Hold
        {
            EvaluationId = evaluationId,
            CdiHoldId = Guid.NewGuid()
        };

        await _dbFixture.SharedDbContext.Holds.AddAsync(existingHold);
        await _dbFixture.SharedDbContext.SaveChangesAsync();

        var request = new AddHold(new Hold
        {
            EvaluationId = evaluationId,
            CdiHoldId = Guid.NewGuid()
        });

        var subject = CreateSubject();

        var existingHoldCount = await _dbFixture.SharedDbContext.Holds.CountAsync();

        // Act
        var actualResult = await subject.Handle(request, CancellationToken.None);

        // Assert
        Assert.False(actualResult.IsNew);
        Assert.Equal(existingHold, actualResult.Hold);

        Assert.Equal(existingHoldCount, await _dbFixture.SharedDbContext.Holds.CountAsync());
    }
}