using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Signify.FOBT.Svc.Core.Queries;
using Signify.FOBT.Svc.Core.Tests.Utilities;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Queries;

public class GetProviderPayByFobtIdTests : IClassFixture<MockDbFixture>
{
    private readonly GetProviderPayByFobtIdHandler _handler;

    public GetProviderPayByFobtIdTests(MockDbFixture mockDbFixture)
    {
        var logger = A.Fake<ILogger<GetProviderPayByFobtIdHandler>>();
        _handler = new GetProviderPayByFobtIdHandler(mockDbFixture.Context, logger);
    }

    [Fact]
    public async Task GetProviderPayByFobtIdHandler_ReturnsProviderPay()
    {
        // Arrange
        var request = new GetProviderPayByFobtId
        {
            FOBTId = 1
        };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        Assert.Equal("123456ABC", result.PaymentId);
        Assert.Equal(request.FOBTId, result.FOBTId);
    }

    [Fact]
    public async Task GetProviderPayByFobtIdHandler_DoesNotReturnProviderPay()
    {
        // Arrange
        var request = new GetProviderPayByFobtId
        {
            FOBTId = 100
        };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().BeNull();
    }
}