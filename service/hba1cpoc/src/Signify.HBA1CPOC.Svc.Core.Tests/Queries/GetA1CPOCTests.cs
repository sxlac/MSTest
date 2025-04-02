using FluentAssertions;
using Signify.HBA1CPOC.Svc.Core.Queries;
using Signify.HBA1CPOC.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.Queries;

public class GetHBA1CPOCTests : IClassFixture<MockDbFixture>
{
    private readonly GetHBA1CPOCHandler _getHba1CpocHandler;

    public GetHBA1CPOCTests(MockDbFixture mockDbFixture)
    {
        _getHba1CpocHandler = new GetHBA1CPOCHandler(mockDbFixture.Context);
    }

    [Theory]
    [InlineData(324357)]
    [InlineData(324358)]
    [InlineData(324356)]
    public async Task GetHBA1CPOC_ReturnsOneResult_Successful(int evaluationId)
    {
        // Arrange
        var query = new GetHBA1CPOC() { EvaluationId = evaluationId };
            
        // Act
        var result = await _getHba1CpocHandler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        result.HBA1CPOCId.Equals(evaluationId);
    }

    [Theory]
    [InlineData(1231)]
    [InlineData(1232)]
    [InlineData(1233)]
    public async Task GetHBA1CPOC_Type_Datanotfound(int evaluationId)
    {
        // Arrange
        var query = new GetHBA1CPOC() { EvaluationId = evaluationId };

        // Act
        var result = await _getHba1CpocHandler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(324357)]
    [InlineData(324358)]
    [InlineData(324356)]
    public async Task GetHBA1CPOC_Type_Datafound(int evaluationId)
    {
        // Arrange
        var query = new GetHBA1CPOC() { EvaluationId = evaluationId };

        // Act
        var result = await _getHba1CpocHandler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<Core.Data.Entities.HBA1CPOC>();
    }

    [Theory]
    [InlineData(1231)]
    [InlineData(1232)]
    [InlineData(1233)]
    public async Task GetHBA1CPOC_Returns_NullResult(int evaluationId)
    {
        // Arrange
        var query = new GetHBA1CPOC() { EvaluationId = evaluationId };

        // Act
        var result = await _getHba1CpocHandler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }
}