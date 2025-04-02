using FluentAssertions;
using Signify.FOBT.Svc.Core.Data.Entities;
using Signify.FOBT.Svc.Core.Queries;
using Signify.FOBT.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Queries;

public class GetFobtStatusByStatusCodeAndEvaluationIdTests : IClassFixture<MockDbFixture>
{
    private readonly GetFobtStatusByStatusCodeAndEvaluationIdHandler _handler;

    public GetFobtStatusByStatusCodeAndEvaluationIdTests(MockDbFixture mockDbFixture)
    {
        _handler = new GetFobtStatusByStatusCodeAndEvaluationIdHandler(mockDbFixture.Context);
    }

    [Fact]
    public async Task Handler_WhenNoValueFound_ReturnNull()
    {
        // Arrange
        var request = new GetFobtStatusByStatusCodeAndEvaluationId 
        { 
            FobtStatusCode = FOBTStatusCode.OrderUpdated,
            EvaluationId = 123456
        };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handler_WhenValueFound_ReturnExpectedValue()
    {
        // Arrange
        var request = new GetFobtStatusByStatusCodeAndEvaluationId
        {
            FobtStatusCode = FOBTStatusCode.OrderUpdated,
            EvaluationId = 324356
        };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().NotBeNull();
        result.FOBTStatusCode.FOBTStatusCodeId.Should().Be(FOBTStatusCode.OrderUpdated.FOBTStatusCodeId);
    }
}