using Signify.FOBT.Svc.Core.Queries;
using Signify.FOBT.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using Xunit;

namespace Signify.FOBT.Svc.Core.Tests.Queries;

public class QueryFOBTWithIdTests : IClassFixture<MockDbFixture>
{
    private readonly QueryFOBTWithIdHandler _handler;

    public QueryFOBTWithIdTests(MockDbFixture mockDbFixture)
    {
        _handler = new QueryFOBTWithIdHandler(mockDbFixture.Context);
    }

    [Fact]
    public async Task QueryFOBTWithIdHandler_ReturnFobtExam()
    {
        // Arrange
        var request = new QueryFOBTWithId
        {
            FOBTId = 1
        };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        Assert.Equal(request.FOBTId, result.FOBT.FOBTId);
        Assert.Equal(324356, result.FOBT.EvaluationId);
    }

    [Fact]
    public async Task QueryFOBTWithIdHandler_DoesNotReturnFobtExam()
    {
        // Arrange
        var request = new QueryFOBTWithId
        {
            FOBTId = 100
        };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        Assert.Null(result.FOBT);
        Assert.Equal(QueryFOBTStatus.NotFound, result.Status);
    }
}