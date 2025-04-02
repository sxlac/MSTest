using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Signify.PAD.Svc.Core.Data.Entities;
using Signify.PAD.Svc.Core.Queries;
using Signify.PAD.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Queries;

public class GetPadByEvaluationIdAndStatusCodeTests : IClassFixture<MockDbFixture>
{
    private readonly GetPadByEvaluationIdAndStatusCodeHandler _handler;

    public GetPadByEvaluationIdAndStatusCodeTests(MockDbFixture mockDbFixture)
    {
        var logger = A.Fake<ILogger<GetPadByEvaluationIdAndStatusCodeHandler>>();
        _handler = new GetPadByEvaluationIdAndStatusCodeHandler(mockDbFixture.Context, logger);
    }

    [Theory]
    [InlineData(324356)]
    public async Task Handle_ReturnsOneResult_Successful(int evaluationId)
    {
        var query = new GetPadByEvaluationIdAndStatusCode
        {
            EvaluationId = evaluationId,
            StatusCode = PADStatusCode.PadPerformed.PADStatusCodeId
        };
        var result = await _handler.Handle(query, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(1, result.PADId);
        result.Should().BeOfType<PADStatus>();
    }

    [Theory]
    [InlineData(324357)]
    public async Task Handle_Returns_NullResult_When_Status_Absent(int evaluationId)
    {
        var query = new GetPadByEvaluationIdAndStatusCode
        {
            EvaluationId = evaluationId,
            StatusCode = PADStatusCode.PadPerformed.PADStatusCodeId
        };
        var result = await _handler.Handle(query, CancellationToken.None);
        Assert.Null(result);
    }
}