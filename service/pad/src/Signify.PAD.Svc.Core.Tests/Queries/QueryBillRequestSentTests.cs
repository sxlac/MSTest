using Microsoft.EntityFrameworkCore;
using Signify.PAD.Svc.Core.Queries;
using Signify.PAD.Svc.Core.Tests.Utilities;
using System.Threading.Tasks;
using Xunit;

namespace Signify.PAD.Svc.Core.Tests.Queries;

public class QueryBillRequestSentTests : IClassFixture<MockDbFixture>
{
    private readonly QueryBillRequestSentHandler _subject;

    public QueryBillRequestSentTests(MockDbFixture fixture)
    {
        _subject = new QueryBillRequestSentHandler(fixture.Context);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(100, false)]
    public async Task Handle_Tests(int padId, bool expectedExists)
    {
        using var fixture = new MockDbFixture();

        #region Ensure test data is configured correctly
        var exists = await fixture.Context.PADRCMBilling
            .AsNoTracking()
            .Include(each => each.PAD)
            .AnyAsync(each => each.PAD.PADId == padId);

        // Unfortunately Assert.Equals doesn't have an override that accepts the user message
        if (expectedExists)
            Assert.True(exists, "Test data is not configured correctly");
        else
            Assert.False(exists, "Test data is not configured correctly");
        #endregion

        var actualExists = await _subject.Handle(new QueryBillRequestSent(padId), default);

        Assert.Equal(expectedExists, actualExists);
    }
}