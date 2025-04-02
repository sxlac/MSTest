using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Signify.FOBT.Svc.Core.Queries;
using Signify.FOBT.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using System;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Queries;

public class GetLabResultTests : IClassFixture<MockDbFixture>
{
    private readonly GetLabResultHandler _handler;

    public GetLabResultTests(MockDbFixture mockDbFixture)
    {
        var logger = A.Fake<ILogger<GetLabResultHandler>>();
        _handler = new GetLabResultHandler(mockDbFixture.Context, logger);
    }

    [Fact]
    public async Task GetFobtByOrderCorrelationId_ReturnsLabResult()
    {
        // Arrange
        var request = new GetLabResult
        {
            OrderCorrelationId = new Guid("b65e62ed-fd2c-4b7f-b183-0e70973c1fe6")
        };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        Assert.Equal(1, result.LabResultId);
        Assert.Equal(request.OrderCorrelationId, result.OrderCorrelationId);
    }

    [Fact]
    public async Task GetFobtByOrderCorrelationId_NoLabResultFound()
    {
        // Arrange
        var request = new GetLabResult
        {
            OrderCorrelationId = Guid.Empty
        };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().BeNull();
    }
}