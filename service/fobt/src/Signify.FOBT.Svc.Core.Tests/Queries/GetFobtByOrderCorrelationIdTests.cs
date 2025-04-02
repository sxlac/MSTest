using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Signify.FOBT.Svc.Core.Queries;
using Signify.FOBT.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using System;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Queries;

public class GetFobtByOrderCorrelationIdTests : IClassFixture<MockDbFixture>
{
    private readonly GetFobtByOrderCorrelationIdHandler _handler;

    public GetFobtByOrderCorrelationIdTests(MockDbFixture mockDbFixture)
    {
        var logger = A.Fake<ILogger<GetFobtByOrderCorrelationId>>();
        _handler = new GetFobtByOrderCorrelationIdHandler(mockDbFixture.Context, logger);
    }

    [Fact]
    public async Task GetFobtByOrderCorrelationId_ReturnFobtExam()
    {
        // Arrange
        var request = new GetFobtByOrderCorrelationId
        {
            OrderCorrelationId = new Guid("e5bb25b9-3a01-4f28-b4dc-14408c902078")
        };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        Assert.Equal(324356, result.EvaluationId);
        Assert.Equal(request.OrderCorrelationId, result.OrderCorrelationId);
    }

    [Fact]
    public async Task GetFobtByOrderCorrelationId_NoFobtExamFound()
    {
        // Arrange
        var request = new GetFobtByOrderCorrelationId
        {
            OrderCorrelationId = Guid.Empty
        };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().BeNull();
    }
}