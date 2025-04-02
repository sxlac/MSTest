using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Signify.FOBT.Svc.Core.Queries;
using Signify.FOBT.Svc.Core.Tests.Mocks.Models;
using Signify.FOBT.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Queries;

public class GetFobtByFobtIdTests : IClassFixture<MockDbFixture>
{
    private readonly GetFobtByFobtIdHandler _handler;

    public GetFobtByFobtIdTests(MockDbFixture mockDbFixture)
    {
        var logger = A.Fake<ILogger<GetFobtByFobtIdHandler>>();

        _handler = new GetFobtByFobtIdHandler(mockDbFixture.Context, logger);
    }

    [Fact]
    public async Task Handle_RequestContainsInvalidFobtData_ReturnsNull()
    {
        // Arrange
        var request = new GetFobtByFobtId
        {
            FobtId = 987654321
        };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_RequestContainsValidFobtData_ReturnFobtRecord()
    {
        // Arrange
        var mockFobt = FobtEntityMock.BuildFobt();
        var request = new GetFobtByFobtId
        {
            FobtId = mockFobt.FOBTId
        };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().NotBeNull();
        result.FOBTId.Should().Be(mockFobt.FOBTId);
    }
}