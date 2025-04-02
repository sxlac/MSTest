using Signify.PAD.Svc.Core.Queries;
using Signify.PAD.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Queries;

public class QueryPadPerformedStatusTests : IClassFixture<MockDbFixture>
{
    private readonly QueryPadPerformedStatusHandler _subject;

    public QueryPadPerformedStatusTests(MockDbFixture fixture)
    {
        _subject = new QueryPadPerformedStatusHandler(fixture.Context);
    }

    [Fact]
    public async Task Handle_WhenNoStatusExists_ReturnsNullIsPerformed()
    {
        var actual = await _subject.Handle(new QueryPadPerformedStatus(0), default);

        Assert.Null(actual.IsPerformed);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public async Task Handle_WhenStatusExists_Tests(int padId, bool expectedIsPerformed)
    {
        var actual = await _subject.Handle(new QueryPadPerformedStatus(padId), default);

        Assert.Equal(expectedIsPerformed, actual.IsPerformed);
    }
}