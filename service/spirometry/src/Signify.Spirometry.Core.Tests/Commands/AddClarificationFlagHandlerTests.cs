using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Data.Entities;
using System.Linq;
using System.Threading.Tasks;
using System;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Commands;

public sealed class AddClarificationFlagHandlerTests : IDisposable, IAsyncDisposable
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

    private AddClarificationFlagHandler CreateSubject() => new(_dbFixture.SharedDbContext);

    [Fact]
    public async Task Handle_HappyPath()
    {
        // Arrange
        var request = new AddClarificationFlag(new ClarificationFlag());

        // Act
        var actual = await CreateSubject().Handle(request, default);

        // Assert
        Assert.NotNull(actual);
        Assert.Single(_dbFixture.SharedDbContext.ClarificationFlags);
        Assert.Equal(actual, _dbFixture.SharedDbContext.ClarificationFlags.First());
    }
}