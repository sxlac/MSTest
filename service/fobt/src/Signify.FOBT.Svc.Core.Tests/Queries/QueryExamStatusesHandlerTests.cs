using Signify.FOBT.Svc.Core.Queries;
using Signify.FOBT.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Queries;

public class QueryExamStatusesHandlerTests
{
    [Fact]
    public async Task Handle_WhenNoStatusesExist_ReturnsEmptyCollection()
    {
        // Arrange
        const int examId = 99;

        var request = new QueryExamStatuses
        {
            ExamId = examId
        };

        await using var fixture = new MockDbFixture();

        // Act
        var result = await new QueryExamStatusesHandler(fixture.Context).Handle(request, default);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_WhenStatusesExist_ReturnsStatuses()
    {
        // Arrange
        const int examId = 1;

        var request = new QueryExamStatuses
        {
            ExamId = examId
        };

        await using var fixture = new MockDbFixture();

        // Act
        var result = await new QueryExamStatusesHandler(fixture.Context).Handle(request, default);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
    }
}