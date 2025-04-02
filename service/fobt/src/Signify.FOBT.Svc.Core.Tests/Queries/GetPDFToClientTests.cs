using FluentAssertions;
using Signify.FOBT.Svc.Core.Messages.Queries;
using Signify.FOBT.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Queries;

public class GetPDFToClientTest : IClassFixture<MockDbFixture>
{
    private readonly GetPDFToClientHandler _handler;

    public GetPDFToClientTest(MockDbFixture mockDbFixture)
    {
        _handler = new GetPDFToClientHandler(mockDbFixture.Context);
    }

    [Fact]
    public async Task GetLabResultByFobtIdHandler_WhenNoValueFound_ReturnNull()
    {
        // Arrange
        const int evaluationId = 123;
        const int fobtId = 123;
        var request = new GetPDFToClient(fobtId, evaluationId);
         
        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLabResultByFobtIdHandler_WhenValueFound_ReturnExpectedValue()
    {
        // Arrange
        // Arrange
        const int evaluationId = 1;
        const int fobtId = 1;
        var request = new GetPDFToClient(fobtId, evaluationId);

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Should().NotBeNull();
        result.FOBTId.Should().Be(fobtId);
    }
}