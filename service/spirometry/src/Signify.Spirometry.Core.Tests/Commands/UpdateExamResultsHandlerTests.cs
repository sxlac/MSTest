using Microsoft.EntityFrameworkCore;
using Signify.Spirometry.Core.Commands;
using Signify.Spirometry.Core.Data.Entities;
using System.Threading.Tasks;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Commands;

public class UpdateExamResultsHandlerTests
{   
    private static UpdateExamResultsHandler CreateSubject(MockDbFixture fixture) => new(fixture.SharedDbContext);

    /// <summary>
    /// We can't really unit test `Update` was called, but we know if you
    /// call `Update` with an entity that's not tracked, it will set it's
    /// state to `Add`
    /// </summary>
    [Fact]
    public async Task Handle_HappyPath()
    {
        // Arrange
        await using var fixture = new MockDbFixture();

        var request = new UpdateExamResults(new SpirometryExamResult());

        var existingCount = await CountResults();

        // Act
        await CreateSubject(fixture).Handle(request, default);

        // Assert
        Assert.Equal(existingCount + 1, await CountResults());

        Task<int> CountResults()
            => fixture.SharedDbContext.SpirometryExamResults.CountAsync();
    }
}