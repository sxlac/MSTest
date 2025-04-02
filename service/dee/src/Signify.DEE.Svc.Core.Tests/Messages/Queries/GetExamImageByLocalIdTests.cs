using FluentAssertions;
using Signify.DEE.Svc.Core.Messages.Queries;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Messages.Queries;

public class GetExamImageByLocalIdTests(MockDbFixture mockDbFixture) : IClassFixture<MockDbFixture>
{
    private readonly GetExamImageByLocalIdHandler _handler = new(mockDbFixture.FakeDatabaseContext);

    [Fact]
    public async Task GetExamImageByLocalId_ReturnsEntity_WhenLocalIdMatched()
    {
        // Arrange
        var request = new GetExamImageByLocalId { LocalId = "55" };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetExamImageByLocalId_ReturnsNull_WhenLocalIdNotMatched()
    {
        // Arrange
        var request = new GetExamImageByLocalId { LocalId = "38" };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().BeNull();
    }
}