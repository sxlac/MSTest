using FluentAssertions;
using Signify.DEE.Svc.Core.Messages.Queries;
using System.Threading.Tasks;
using Xunit;

namespace Signify.DEE.Svc.Core.Tests.Messages.Queries;

public class GetExamByImageLocalIdTests(MockDbFixture mockDbFixture) : IClassFixture<MockDbFixture>
{
    private readonly GetExamByImageLocalIdHandler _handler = new(mockDbFixture.FakeDatabaseContext);

    [Fact]
    public async Task  GetExamByImageLocalId_ReturnsEntity_WhenLocalIdMatched()
    {
        // Arrange
        var request = new  GetExamByImageLocalId { ImageLocalId = "55" };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task  GetExamByImageLocalId_ReturnsNull_WhenLocalIdNotMatched()
    {
        // Arrange
        var request = new  GetExamByImageLocalId { ImageLocalId = "38" };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().BeNull();
    }
}