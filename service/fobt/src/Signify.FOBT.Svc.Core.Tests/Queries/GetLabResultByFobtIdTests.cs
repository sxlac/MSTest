using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Signify.FOBT.Svc.Core.Queries;
using Signify.FOBT.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Queries;

public class GetLabResultByFobtIdTests : IClassFixture<MockDbFixture>
{
    private readonly GetLabResultByFobtIdHandler _handler;

    public GetLabResultByFobtIdTests(MockDbFixture mockDbFixture)
    {
        var logger = A.Fake<ILogger<GetLabResultHandler>>();

        _handler = new GetLabResultByFobtIdHandler(mockDbFixture.Context, logger);
    }

    [Fact]
    public async Task GetLabResultByFobtIdHandler_WhenNoValueFound_ReturnNull()
    {
        // Arrange
        var request = new GetLabResultByFobtId 
        { 
            FobtId = 9876
        };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLabResultByFobtIdHandler_WhenValueFound_ReturnExpectedValue()
    {
        // Arrange
        const int fobtId = 1;
        var request = new GetLabResultByFobtId
        {
            FobtId = fobtId
        };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().NotBeNull();
        result.FOBTId.Should().Be(fobtId);
    }
}