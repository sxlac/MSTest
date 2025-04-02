using Signify.Spirometry.Core.Data.Entities;
using Signify.Spirometry.Core.Queries;
using System.Threading.Tasks;
using Xunit;

namespace Signify.Spirometry.Core.Tests.Queries;

public class QueryExamStatusTests
{
    [Fact]
    public async Task Handle_WhenEntityDoesNotExist_ReturnsNull()
    {
        // Arrange
        const int spirometryExamId = 1;

        await using var fixture = new MockDbFixture();

        var subject = new QueryExamStatusHandler(fixture.SharedDbContext);

        // Act
        var result = await subject.Handle(new QueryExamStatus
        {
            SpirometryExamId = spirometryExamId,
            StatusCode = StatusCode.SpirometryExamPerformed
        }, default);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WhenEntityExists_ReturnsEntity()
    {
        // Arrange
        const int spirometryExamId = 1;

        var statusCode = StatusCode.SpirometryExamPerformed;

        await using var fixture = new MockDbFixture();

        var status = new ExamStatus
        {
            SpirometryExamId = spirometryExamId,
            StatusCodeId = statusCode.StatusCodeId
        };

        await fixture.SharedDbContext.ExamStatuses.AddAsync(status);
        await fixture.SharedDbContext.SaveChangesAsync();

        var subject = new QueryExamStatusHandler(fixture.SharedDbContext);

        // Act
        var result = await subject.Handle(new QueryExamStatus
        {
            SpirometryExamId = spirometryExamId,
            StatusCode = statusCode
        }, default);

        // Assert
        Assert.NotNull(result);
    }
}