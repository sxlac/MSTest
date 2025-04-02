using FluentAssertions;
using Signify.HBA1CPOC.Svc.Core.Queries;
using Signify.HBA1CPOC.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.HBA1CPOC.Svc.Core.Tests.Queries;

public class GetPdfToClientHandlerTests : IClassFixture<MockDbFixture>
{
    private readonly GetPdfToClientHandler _handler;

    public GetPdfToClientHandlerTests(MockDbFixture mockDbFixture)
    {
        _handler = new GetPdfToClientHandler(mockDbFixture.Context);
    }

    [Theory]
    [InlineData(324357)]
    [InlineData(324358)]
    [InlineData(324356)]
    public async Task GetPdfToClientHandle_RequestCorrectEvaluationId_SuccessfulResponse(int evaluationId)
    {
        // Arrange
        var query = new GetPdfToClient { EvaluationId = evaluationId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        result.EvaluationId.Equals(evaluationId);
    }

    [Theory]
    [InlineData(1231)]
    [InlineData(1232)]
    [InlineData(1233)]
    public async Task GetHBA1CPOC_RequestIncorrectEvaluationIdAndValidStatusCode_NullResponse(int evaluationId)
    {
        // Arrange
        var query = new GetPdfToClient { EvaluationId = evaluationId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}