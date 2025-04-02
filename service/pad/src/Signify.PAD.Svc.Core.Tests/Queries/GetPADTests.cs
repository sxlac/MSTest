using FluentAssertions;
using Signify.PAD.Svc.Core.Queries;
using Signify.PAD.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Queries;

public class GetPADTests : IClassFixture<MockDbFixture>
{
    private readonly GetPADHandler _handler;

    public GetPADTests(MockDbFixture mockDbFixture)
    {
        _handler = new GetPADHandler(mockDbFixture.Context);
    }
    
    [Fact]
    public async Task Handle_WhenExists_ReturnsResult()
    {
        var query = new GetPAD { EvaluationId = 324356 };
        var result = await _handler.Handle(query, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(1, result.PADId);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNull()
    {
        var query = new GetPAD { EvaluationId = 1231 };
        var result = await _handler.Handle(query, CancellationToken.None);
        result.Should().BeNull();
    }
}