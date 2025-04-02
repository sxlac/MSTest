using System.Threading;
using System.Threading.Tasks;
using Signify.CKD.Svc.Core.Queries;
using Signify.CKD.Svc.Core.Tests.Utilities;
using Xunit;

namespace Signify.CKD.Svc.Core.Tests.Queries;

public class GetCKDTests : IClassFixture<MockDbFixture>
{
    private readonly GetCKDHandler _getCkdHandler;

    public GetCKDTests(MockDbFixture mockDbFixture)
    {
        _getCkdHandler = new GetCKDHandler(mockDbFixture.Context);
    }

    [Theory]
    [InlineData(324357)]
    [InlineData(324358)]
    [InlineData(324356)]
    public async Task Handle_WhenRecordExists_ReturnsRecord(int evaluationId)
    {
        var query = new GetCKD { EvaluationId = evaluationId };
        var result = await _getCkdHandler.Handle(query, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(evaluationId, result.EvaluationId);
    }

    [Theory]
    [InlineData(1231)]
    [InlineData(1232)]
    [InlineData(1233)]
    public async Task Handle_WhenRecordNotFound_ReturnsNull(int evaluationId)
    {
        var query = new GetCKD { EvaluationId = evaluationId };
        var result = await _getCkdHandler.Handle(query, CancellationToken.None);
        Assert.Null(result);
    }
}