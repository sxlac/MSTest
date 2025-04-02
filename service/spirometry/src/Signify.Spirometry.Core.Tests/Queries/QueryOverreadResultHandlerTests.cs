using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Queries;
using System.Threading.Tasks;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Queries;

public class QueryOverreadResultHandlerTests
{
    [Fact]
    public async Task Handle_WhenEntityDoesNotExist_ReturnsNull()
    {
        // Arrange
        const long appointmentId = 1;

        await using var fixture = new MockDbFixture();

        var request = new QueryOverreadResult(appointmentId);

        // Act
        var subject = new QueryOverreadResultHandler(fixture.SharedDbContext);

        var actual = await subject.Handle(request, default);

        // Assert
        Assert.Null(actual);
    }

    [Fact]
    public async Task Handle_WhenEntityExists_ReturnsEntity()
    {
        // Arrange
        const long appointmentId = 1;

        await using var fixture = new MockDbFixture();

        var entity = CreateDummyOverreadResult();
        entity.AppointmentId = appointmentId;

        await fixture.SharedDbContext.OverreadResults.AddAsync(entity);
        await fixture.SharedDbContext.SaveChangesAsync();

        var request = new QueryOverreadResult(appointmentId);

        // Act
        var subject = new QueryOverreadResultHandler(fixture.SharedDbContext);

        var actual = await subject.Handle(request, default);

        // Assert
        Assert.NotNull(actual);
    }

    private static OverreadResult CreateDummyOverreadResult()
    {
        // These types are nullable, but EF knows they're not nullable in the db, so must
        // be set to add to the db context
        return new OverreadResult
        {
            BestFev1TestComment = string.Empty,
            BestFvcTestComment = string.Empty,
            BestPefTestComment = string.Empty,
            OverreadBy = string.Empty,
            OverreadComment = string.Empty
        };
    }
}