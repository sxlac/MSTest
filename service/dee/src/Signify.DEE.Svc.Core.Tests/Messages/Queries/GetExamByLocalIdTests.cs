using FluentAssertions;
using Signify.DEE.Svc.Core.Messages.Queries;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Messages.Queries;

public class GetExamByLocalIdTests(MockDbFixture mockDbFixture) : IClassFixture<MockDbFixture>
{
    private readonly GetExamByLocalIdHandler _handler = new(mockDbFixture.FakeDatabaseContext);

    [Fact]
    public async Task GetExamByLocalId_ReturnsEntity_WhenLocalIdMatched()
    {
        // Arrange
        var request = new GetExamByLocalId { LocalId = "85" };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetExamByLocalId_ReturnsNull_WhenLocalIdNotMatched()
    {
        // Arrange
        var request = new GetExamByLocalId { LocalId = "38" };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().BeNull();
    }
}